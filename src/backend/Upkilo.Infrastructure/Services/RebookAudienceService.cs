using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

/// <summary>Why a past visit will or will not produce a rebooking reminder.</summary>
public enum RebookEligibility
{
    /// <summary>Due, contactable and consented — a message will go out.</summary>
    Ready = 0,

    /// <summary>The client already has a later booking for this same service.</summary>
    AlreadyRebooked = 1,

    /// <summary>The client has not opted in to marketing on any channel we could use.</summary>
    NoConsent = 2,

    /// <summary>Consented, but we hold no email address or phone number to reach them on.</summary>
    NoContactDetails = 3,
}

/// <summary>One past visit that has passed its service's rebooking interval.</summary>
public record RebookCandidate(
    Guid BookingId,
    Guid TenantId,
    Guid ClientId,
    string ClientName,
    Guid ServiceId,
    string ServiceName,
    DateTime LastVisit,
    int DaysSinceVisit,
    int RebookAfterDays,
    RebookEligibility Eligibility,
    /// <summary>"email", "sms", or null when nothing would be sent.</summary>
    string? Channel);

/// <summary>
/// Works out who is due to rebook, and for which service.
///
/// This exists as a shared service rather than living inside the job because two callers need the
/// same answer: the job, which sends, and the retargeting audience endpoint, which shows the
/// tenant who it is about to contact. If those two computed eligibility separately they would
/// drift, and a preview that disagrees with what actually sends is worse than no preview — the
/// tenant would be reviewing an audience that is not the one being messaged.
///
/// Targeting is per SERVICE, not per client. A client who had a colour and a massage is assessed
/// once for each: the interval comes from the service they actually booked, and the
/// already-rebooked check is scoped to that same service. Rebooking a massage therefore does not
/// suppress the reminder that their colour is due.
/// </summary>
public class RebookAudienceService
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Visits older than this are left alone. Without it, switching the feature on would contact
    /// every dormant client in the database in a single night.
    /// </summary>
    public static readonly TimeSpan MaxOverdue = TimeSpan.FromDays(90);

    public RebookAudienceService(AppDbContext context) => _context = context;

    /// <summary>
    /// Visits now past their service's rebooking interval, each tagged with whether a message
    /// would actually go out and why not.
    /// </summary>
    /// <param name="tenantId">Restrict to one tenant (the audience endpoint). Null scans all (the job).</param>
    /// <param name="limit">Upper bound on rows examined, so one large tenant cannot monopolise a run.</param>
    public async Task<List<RebookCandidate>> GetDueAsync(Guid? tenantId, int limit, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var earliest = now - MaxOverdue;

        var pausedTenantIds = await _context.Tenants
            .Where(t => !t.RebookRemindersEnabled)
            .Select(t => t.Id)
            .ToListAsync(ct);

        // The SQL filter uses only constants. The obvious formulation —
        //   b.StartTime.AddDays(b.Service.RebookAfterDays.Value) <= now
        // adds a column-valued interval, whose translation depends on the provider; if it failed
        // to translate it would either throw at runtime or silently evaluate client-side over the
        // whole table. The per-service due date is therefore computed in memory below, against a
        // window guaranteed to contain every due booking: nothing is due sooner than one day
        // after the visit, and MaxOverdue bounds the other end.
        var query = _context.Bookings
            .Where(b => !pausedTenantIds.Contains(b.TenantId))
            .Where(b => b.Status == BookingStatus.Completed)
            .Where(b => b.RebookReminderSentAt == null)
            .Where(b => b.ClientId != null)
            .Where(b => b.ServiceId != null && b.Service!.RebookAfterDays != null)
            .Where(b => b.StartTime >= earliest && b.StartTime <= now.AddDays(-1));

        if (tenantId.HasValue)
            query = query.Where(b => b.TenantId == tenantId.Value);

        // Projected to the twelve fields actually used, instead of Include-ing whole entities.
        //
        // This was .Include(b => b.Service).Include(b => b.Client) followed by ToListAsync, which
        // materialises EVERY column of Bookings, Services and Clients. Production logged it at
        // 1893ms, the slowest query on the platform, while the code below reads nine values from
        // it. The cost was transfer and materialisation, not database CPU — the server sits at
        // ~15% CPU with its burst credits untouched, so no amount of extra hardware would have
        // helped.
        //
        // FullName is composed here rather than in the projection: it is a computed property on
        // Client, not a mapped column, so EF cannot translate it.
        var window = await query
            .OrderBy(b => b.StartTime)
            .Take(limit)
            .Select(b => new DueRow(
                b.Id,
                b.TenantId,
                b.ClientId!.Value,
                b.ServiceId!.Value,
                b.StartTime,
                b.Client!.FirstName,
                b.Client.LastName,
                b.Client.MarketingConsent,
                b.Client.SmsConsent,
                b.Client.Email,
                b.Client.Phone,
                b.Service!.Name,
                b.Service.RebookAfterDays))
            .ToListAsync(ct);

        var due = window
            .Where(r => r.RebookAfterDays is int days && r.StartTime.AddDays(days) <= now)
            .OrderByDescending(r => r.StartTime)
            .ToList();

        var results = new List<RebookCandidate>();

        // Only the most recent qualifying visit per client+service is a candidate — otherwise a
        // long-standing client would produce one message per historic visit of the same service.
        var seen = new HashSet<(Guid clientId, Guid serviceId)>();

        foreach (var row in due)
        {
            if (!seen.Add((row.ClientId, row.ServiceId))) continue;

            var hasLaterBooking = await _context.Bookings.AnyAsync(b =>
                b.TenantId == row.TenantId &&
                b.ClientId == row.ClientId &&
                b.ServiceId == row.ServiceId &&
                b.StartTime > row.StartTime &&
                b.Status != BookingStatus.Cancelled, ct);

            var (eligibility, channel) = Assess(
                row.MarketingConsent, row.SmsConsent, row.Email, row.Phone, hasLaterBooking);

            var fullName = $"{row.FirstName} {row.LastName}".Trim();

            results.Add(new RebookCandidate(
                row.BookingId,
                row.TenantId,
                row.ClientId,
                string.IsNullOrWhiteSpace(fullName) ? "(unnamed client)" : fullName,
                row.ServiceId,
                row.ServiceName,
                row.StartTime,
                (int)(now - row.StartTime).TotalDays,
                row.RebookAfterDays ?? 0,
                eligibility,
                channel));
        }

        return results;
    }

    /// <summary>
    /// Decides whether this client can be messaged, and on which channel.
    ///
    /// Consent is checked per channel because it is granted per channel: MarketingConsent covers
    /// email and SmsConsent covers SMS, matching what BroadcastController and CampaignsController
    /// already enforce. Email is preferred where both are available — it is the cheaper channel
    /// and the less intrusive one — and only one channel is ever used, so the same nudge does not
    /// arrive twice.
    /// </summary>
    public static (RebookEligibility, string?) Assess(Client client, bool hasLaterBooking) =>
        Assess(client.MarketingConsent, client.SmsConsent, client.Email, client.Phone, hasLaterBooking);

    /// <summary>
    /// The same decision expressed over the four fields it actually reads, so the caller can work
    /// from a projection instead of loading a whole Client row to inspect two booleans and two
    /// strings. The Client overload above is kept and delegates here, so the rule lives in one
    /// place and existing callers are unaffected.
    /// </summary>
    public static (RebookEligibility, string?) Assess(
        bool marketingConsent, bool smsConsent, string? email, string? phone, bool hasLaterBooking)
    {
        if (hasLaterBooking) return (RebookEligibility.AlreadyRebooked, null);

        if (!marketingConsent && !smsConsent) return (RebookEligibility.NoConsent, null);

        if (marketingConsent && !string.IsNullOrWhiteSpace(email)) return (RebookEligibility.Ready, "email");
        if (smsConsent && !string.IsNullOrWhiteSpace(phone)) return (RebookEligibility.Ready, "sms");

        return (RebookEligibility.NoContactDetails, null);
    }

    /// <summary>
    /// The columns one due-booking row actually needs. Deliberately narrow: the query that fills
    /// it was the platform's slowest at 1893ms because it materialised whole Booking, Service and
    /// Client entities to read these twelve values.
    /// </summary>
    private sealed record DueRow(
        Guid BookingId,
        Guid TenantId,
        Guid ClientId,
        Guid ServiceId,
        DateTime StartTime,
        string? FirstName,
        string? LastName,
        bool MarketingConsent,
        bool SmsConsent,
        string? Email,
        string? Phone,
        string ServiceName,
        int? RebookAfterDays);
}
