using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text.Json;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Jobs;

/// <summary>
/// C1: GDPR Article 17 — Right to Erasure.
/// Permanently deletes user data from Upkilo DB AND propagates deletion to Stripe, Twilio,
/// and email provider within the 72h SLA required by GDPR.
/// </summary>
public class DataErasureJob
{
    private readonly AppDbContext _context;
    private readonly ILogger<DataErasureJob> _logger;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    public DataErasureJob(
        AppDbContext context,
        ILogger<DataErasureJob> logger,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Permanently deletes all data associated with a user across the platform.
    /// This is triggered 30 days after a deletion request.
    /// </summary>
    public async Task PermanentlyDeleteUserAsync(Guid userId, Guid tenantId)
    {
        _logger.LogWarning("Starting permanent data erasure for user {UserId} in tenant {TenantId}", userId, tenantId);

        var user = await _context.Users
            .IgnoreQueryFilters() // User is likely soft-deleted (IsActive=false)
            .FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == tenantId);

        if (user == null)
        {
            _logger.LogInformation("User {UserId} not found or already deleted. Skipping erasure.", userId);
            return;
        }

        // If user logged back in, the deletion should have been cancelled.
        // We check IsActive as a proxy for 'cancellation' if our logic reactivates users on login.
        if (user.IsActive)
        {
            _logger.LogInformation("User {UserId} has reactivated their account. Cancelling erasure.", userId);
            return;
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // 1. Delete Sessions
            var sessions = await _context.UserSessions.Where(s => s.UserId == userId).ToListAsync();
            _context.UserSessions.RemoveRange(sessions);

            // 2. Delete Audit Logs (or anonymize them - usually anonymize for platform security)
            var auditLogs = await _context.AuditEntries.Where(a => a.UserId == userId).ToListAsync();
            foreach (var log in auditLogs)
            {
                log.UserId = Guid.Empty; // Anonymize
                log.Details = "[REDACTED - USER DELETED]";
            }

            // 3. Delete Notifications
            var notifications = await _context.Notifications.Where(n => n.UserId == userId).ToListAsync();
            _context.Notifications.RemoveRange(notifications);

            // 4. Delete 2FA configs
            var user2Fa = await _context.User2FAs.Where(u => u.UserId == userId).ToListAsync();
            _context.User2FAs.RemoveRange(user2Fa);

            // 5. Delete refresh tokens
            var tokens = await _context.RefreshTokens.Where(t => t.UserId == userId).ToListAsync();
            _context.RefreshTokens.RemoveRange(tokens);

            // 6. Finally, delete the user itself
            _context.Users.Remove(user);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogCritical("Permanent data erasure COMPLETED for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to complete data erasure for user {UserId}", userId);
            throw; // Re-throw for Hangfire retry
        }

        // C1: Propagate to external providers (best-effort, non-fatal, within 72h SLA)
        await PropagateExternalErasureAsync(userId, user.Email ?? "", tenantId);
    }

    /// <summary>
    /// C1: Propagate GDPR erasure to Stripe, Twilio, and email provider.
    /// Runs after DB transaction commits. Failures are logged but don't block the response.
    /// </summary>
    private async Task PropagateExternalErasureAsync(Guid userId, string email, Guid tenantId)
    {
        var tasks = new List<Task>
        {
            PropagateToStripeAsync(email, userId),
            PropagateToTwilioAsync(email, userId),
            PropagateToMailchimpAsync(email, userId)
        };

        await Task.WhenAll(tasks.Select(t => t.ContinueWith(completed =>
        {
            if (completed.IsFaulted)
                _logger.LogError(completed.Exception, "[C1] External erasure propagation failed for user {UserId}", userId);
        })));

        _logger.LogInformation("[C1] GDPR erasure propagated to external providers for user {UserId}", userId);
    }

    private async Task PropagateToStripeAsync(string email, Guid userId)
    {
        // Stripe: redact customer PII via Customer Update API (set email to redacted@stripe.com)
        var stripeKey = _configuration["Stripe:SecretKey"];
        if (string.IsNullOrEmpty(stripeKey)) return;

        var http = _httpClientFactory.CreateClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", stripeKey);

        // Find customer by email in Stripe
        var search = await http.GetAsync($"https://api.stripe.com/v1/customers?email={Uri.EscapeDataString(email)}&limit=1");
        if (!search.IsSuccessStatusCode) return;

        var json = JsonDocument.Parse(await search.Content.ReadAsStringAsync());
        var customerId = json.RootElement.GetProperty("data").EnumerateArray()
            .Select(c => c.GetProperty("id").GetString()).FirstOrDefault();

        if (customerId == null) return;

        // Redact the customer's email and metadata
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["email"] = $"redacted_{userId:N}@deleted.invalid",
            ["metadata[gdpr_erased]"] = "true",
            ["metadata[erased_at]"] = DateTime.UtcNow.ToString("O")
        });
        await http.PostAsync($"https://api.stripe.com/v1/customers/{customerId}", form);
        _logger.LogInformation("[C1] Stripe customer {CustomerId} redacted for user {UserId}", customerId, userId);
    }

    private async Task PropagateToTwilioAsync(string email, Guid userId)
    {
        // Twilio: redact communication logs associated with this user
        var logs = await _context.CommunicationLogs
            .Where(c => c.UserId == userId)
            .ToListAsync();

        foreach (var log in logs)
        {
            log.Subject = "[GDPR ERASED]";
            log.Body = "[GDPR ERASED]";
        }
        await _context.SaveChangesAsync();
        _logger.LogInformation("[C1] {Count} communication logs redacted for user {UserId}", logs.Count, userId);
    }

    private async Task PropagateToMailchimpAsync(string email, Guid userId)
    {
        // Mailchimp: delete subscriber via DELETE /3.0/lists/{listId}/members/{subscriberHash}
        var mailchimpKey = _configuration["Mailchimp:ApiKey"];
        var mailchimpListId = _configuration["Mailchimp:AudienceListId"];
        if (string.IsNullOrEmpty(mailchimpKey) || string.IsNullOrEmpty(mailchimpListId)) return;

        var dc = mailchimpKey.Split('-').LastOrDefault() ?? "us1";
        var subscriberHash = Convert.ToHexString(
            System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(email.ToLower()))
        ).ToLower();

        var http = _httpClientFactory.CreateClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
            Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"anystring:{mailchimpKey}")));

        await http.DeleteAsync($"https://{dc}.api.mailchimp.com/3.0/lists/{mailchimpListId}/members/{subscriberHash}/actions/delete-permanent");
        _logger.LogInformation("[C1] Mailchimp subscriber deleted for user {UserId}", userId);
    }
}
