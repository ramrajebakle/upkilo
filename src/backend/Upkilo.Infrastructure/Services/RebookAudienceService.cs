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
            .Include(b => b.Service)
            .Include(b => b.Client)
            .Where(b => !pausedTenantIds.Contains(b.TenantId))
            .Where(b => b.Status == BookingStatus.Completed)
            .Where(b => b.RebookReminderSentAt == null)
            .Where(b => b.ServiceId != null && b.Service!.RebookAfterDays != null)
            .Where(b => b.StartTime >= earliest && b.StartTime <= now.AddDays(-1));

        if (tenantId.HasValue)
            query = query.Where(b => b.TenantId == tenantId.Value);

        var window = await query
            .OrderBy(b => b.StartTime)
            .Take(limit)
            .ToListAsync(ct);

        var due = window
            .Where(b => b.Service?.RebookAfterDays is int days && b.StartTime.AddDays(days) <= now)
            .OrderByDescending(b => b.StartTime)
            .ToList();

        var results = new List<RebookCandidate>();

        // Only the most recent qualifying visit per client+service is a candidate — otherwise a
        // long-standing client would produce one message per historic visit of the same service.
        var seen = new HashSet<(Guid clientId, Guid serviceId)>();

        foreach (var booking in due)
        {
            var client = booking.Client;
            var service = booking.Service;
            if (client == null || service == null || booking.ServiceId == null) continue;

            if (!seen.Add((client.Id, booking.ServiceId.Value))) continue;

            var hasLaterBooking = await _context.Bookings.AnyAsync(b =>
                b.TenantId == booking.TenantId &&
                b.ClientId == client.Id &&
                b.ServiceId == booking.ServiceId &&
                b.StartTime > booking.StartTime &&
                b.Status != BookingStatus.Cancelled, ct);

            var (eligibility, channel) = Assess(client, hasLaterBooking);

            results.Add(new RebookCandidate(
                booking.Id,
                booking.TenantId,
                client.Id,
                string.IsNullOrWhiteSpace(client.FullName) ? "(unnamed client)" : client.FullName,
                booking.ServiceId.Value,
                service.Name,
                booking.StartTime,
                (int)(now - booking.StartTime).TotalDays,
                service.RebookAfterDays ?? 0,
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
    public static (RebookEligibility, string?) Assess(Client client, bool hasLaterBooking)
    {
        if (hasLaterBooking) return (RebookEligibility.AlreadyRebooked, null);

        var emailAllowed = client.MarketingConsent;
        var smsAllowed = client.SmsConsent;
        if (!emailAllowed && !smsAllowed) return (RebookEligibility.NoConsent, null);

        if (emailAllowed && !string.IsNullOrWhiteSpace(client.Email)) return (RebookEligibility.Ready, "email");
        if (smsAllowed && !string.IsNullOrWhiteSpace(client.Phone)) return (RebookEligibility.Ready, "sms");

        return (RebookEligibility.NoContactDetails, null);
    }
}
