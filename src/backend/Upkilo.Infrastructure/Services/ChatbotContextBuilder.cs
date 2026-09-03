using System.Text;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// Assembles the knowledge one chatbot turn is allowed to use.
///
/// The chatbot previously had no tenant context at all. Its entire system prompt was
///
///     "Act as a helpful booking assistant for a service business."
///
/// plus the tenant's knowledge base, and a service list only when the visitor's message happened
/// to contain the English substrings "book", "appointment" or "available". So a business that had
/// filled in its name, hours, phone, locations and full price list still had a receptionist that
/// knew none of it, and answered from the model's priors instead - which is exactly how a bot
/// invents opening hours and quotes prices nobody charges.
///
/// Every query here filters on the tenantId passed in. That is deliberate and load-bearing: the
/// global query filter is written as "_tenantId == null || TenantId == _tenantId", so it is
/// DISABLED rather than restrictive when there is no ambient tenant - and the public receptionist
/// endpoint is anonymous, so it runs with exactly that. On that path an explicit predicate is the
/// only thing standing between one salon's visitor and another salon's data.
/// </summary>
public class ChatbotContextBuilder : IChatbotContextBuilder
{
    private readonly AppDbContext _context;
    private readonly IEntitlementService _entitlements;

    // Hard caps. The knowledge base was previously loaded with an unbounded ToListAsync(), so a
    // tenant with a few hundred entries silently produced an enormous prompt: slower, far more
    // expensive per turn, and eventually truncated by the model in whatever order it liked.
    private const int MaxKnowledgeBaseEntries = 40;
    private const int MaxServices = 60;
    private const int MaxLocations = 10;

    public ChatbotContextBuilder(AppDbContext context, IEntitlementService entitlements)
    {
        _context = context;
        _entitlements = entitlements;
    }

    public async Task<ChatbotContext> BuildAsync(
        Guid tenantId, ChatAudience audience, CancellationToken ct = default)
    {
        var tenantFacts = await BuildTenantFactsAsync(tenantId, ct);
        var knowledgeBase = await BuildKnowledgeBaseAsync(tenantId, ct);

        // Platform knowledge is withheld from the public widget on purpose. A salon's customer
        // asking about "plans" means the salon's service menu, not Upkilo's price list; answering
        // with the latter both discloses which vendor the business runs on and puts a software
        // company's billing terms into a conversation the business owns.
        var platformFacts = audience == ChatAudience.TenantStaff
            ? await BuildPlatformFactsAsync(tenantId, ct)
            : string.Empty;

        return new ChatbotContext
        {
            TenantFacts = tenantFacts,
            KnowledgeBase = knowledgeBase,
            PlatformFacts = platformFacts
        };
    }

    private async Task<string> BuildTenantFactsAsync(Guid tenantId, CancellationToken ct)
    {
        var tenant = await _context.Tenants
            .AsNoTracking()
            .Where(t => t.Id == tenantId)
            .Select(t => new
            {
                t.Name,
                t.BusinessName,
                t.Description,
                t.Phone,
                t.Email,
                t.Timezone,
                t.Currency,
                t.Industry,
                t.BusinessType
            })
            .FirstOrDefaultAsync(ct);

        if (tenant == null) return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine($"Business name: {tenant.BusinessName ?? tenant.Name}");

        // Only state what the tenant actually filled in. Emitting "Phone: unknown" invites the
        // model to treat the field as answerable and fill the gap itself.
        AppendIfPresent(sb, "About", tenant.Description);
        AppendIfPresent(sb, "Phone", tenant.Phone);
        AppendIfPresent(sb, "Email", tenant.Email);
        AppendIfPresent(sb, "Industry", tenant.Industry ?? tenant.BusinessType);
        AppendIfPresent(sb, "Timezone", tenant.Timezone);

        var services = await _context.Services
            .AsNoTracking()
            .Where(s => s.TenantId == tenantId && s.IsActive && !s.IsDeleted)
            .OrderBy(s => s.Name)
            .Take(MaxServices)
            .Select(s => new { s.Name, s.Price, s.DurationMinutes, s.Description })
            .ToListAsync(ct);

        if (services.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine($"Services and prices (currency: {tenant.Currency}):");
            foreach (var s in services)
                sb.AppendLine($"- {s.Name}: {s.Price} {tenant.Currency}, {s.DurationMinutes} minutes");
        }

        var locations = await _context.Set<Location>()
            .AsNoTracking()
            .Where(l => l.TenantId == tenantId && !l.IsDeleted)
            .Take(MaxLocations)
            .Select(l => new { l.Name, l.AddressLine1, l.AddressLine2, l.City, l.PostalCode, l.Phone })
            .ToListAsync(ct);

        if (locations.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Locations:");
            foreach (var l in locations)
            {
                var address = Join(l.AddressLine1, l.AddressLine2, l.City, l.PostalCode);
                var phone = string.IsNullOrWhiteSpace(l.Phone) ? "" : $" (tel {l.Phone})";
                sb.AppendLine($"- {l.Name}: {address}{phone}");
            }
        }

        return sb.ToString().TrimEnd();
    }

    private async Task<string> BuildKnowledgeBaseAsync(Guid tenantId, CancellationToken ct)
    {
        var entries = await _context.AIKnowledgeBases
            .AsNoTracking()
            .Where(k => k.TenantId == tenantId && k.IsActive && !k.IsDeleted)
            .OrderByDescending(k => k.UpdatedAt)
            .Take(MaxKnowledgeBaseEntries)
            .Select(k => new { k.Question, k.Answer })
            .ToListAsync(ct);

        if (entries.Count == 0) return string.Empty;

        var sb = new StringBuilder();
        foreach (var e in entries)
            sb.AppendLine($"Q: {e.Question}\nA: {e.Answer}");

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Upkilo platform facts, derived from the live pricing catalogue and this tenant's resolved
    /// entitlements rather than written into a string here. A hardcoded feature list would start
    /// drifting from the catalogue the day a plan changed, and the assistant would confidently
    /// describe a product that no longer exists.
    /// </summary>
    private async Task<string> BuildPlatformFactsAsync(Guid tenantId, CancellationToken ct)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Upkilo is the software platform this business runs on.");

        // Monthly price comes from the PlanPrice rows, which is where it actually lives. A plan
        // with no monthly price configured is listed without one rather than with a zero - a
        // wrong price is worse than an absent one.
        var plans = await _context.PricingPlans
            .AsNoTracking()
            .Where(p => p.IsActive && !p.IsDeleted)
            .Select(p => new
            {
                p.Name,
                p.Description,
                Monthly = p.Prices
                    .Where(pr => pr.Cycle == BillingCycle.Monthly)
                    .Select(pr => (decimal?)pr.Amount)
                    .FirstOrDefault(),
                Currency = p.Prices
                    .Where(pr => pr.Cycle == BillingCycle.Monthly)
                    .Select(pr => pr.CurrencyCode)
                    .FirstOrDefault()
            })
            .ToListAsync(ct);

        if (plans.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Upkilo subscription plans:");
            foreach (var p in plans.OrderBy(p => p.Monthly ?? decimal.MaxValue))
            {
                sb.AppendLine(p.Monthly.HasValue
                    ? $"- {p.Name}: {p.Monthly} {p.Currency} per month"
                    : $"- {p.Name} (contact Upkilo for pricing)");
            }
        }

        // What THIS tenant is actually entitled to, so "can I use X" is answered from the same
        // engine that enforces it rather than from the marketing list of what exists.
        var entitlements = await _entitlements.GetEffectiveEntitlementsAsync(tenantId, ct);
        var enabled = entitlements.Features
            .Where(f => f.Value.IsEnabled)
            .Select(f => f.Key)
            .OrderBy(k => k)
            .ToList();

        sb.AppendLine();
        sb.AppendLine(enabled.Count > 0
            ? $"Features enabled on this business's current plan: {string.Join(", ", enabled)}"
            : "This business currently has no paid features enabled.");

        return sb.ToString().TrimEnd();
    }

    private static void AppendIfPresent(StringBuilder sb, string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value)) sb.AppendLine($"{label}: {value.Trim()}");
    }

    private static string Join(params string?[] parts) =>
        string.Join(", ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
}
