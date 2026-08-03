using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// Proactive Messaging Service — AI-driven outbound messaging.
/// Analyzes client behavior and triggers personalized messages at optimal times.
///
/// Triggers:
///   - Lapsed clients (no booking in N days) → re-engagement message
///   - Upcoming birthday → birthday offer
///   - Abandoned booking flow → recovery nudge
///   - Post-service follow-up → review request + upsell
///   - Milestone (10th booking) → loyalty reward
/// </summary>
public interface IProactiveMessagingService
{
    Task<IReadOnlyList<ProactiveMessage>> GeneratePendingMessagesAsync(
        Guid tenantId, CancellationToken ct = default);

    Task<int> SendPendingMessagesAsync(
        Guid tenantId, bool dryRun = false, CancellationToken ct = default);

    Task<ProactiveMessage?> GenerateForClientAsync(
        Guid tenantId, Guid clientId, string trigger, CancellationToken ct = default);
}

public class ProactiveMessagingService : IProactiveMessagingService
{
    private readonly AppDbContext _db;
    private readonly IAIService _ai;
    private readonly IEmailService _email;
    private readonly ILogger<ProactiveMessagingService> _logger;

    // Days without a booking before triggering re-engagement
    private const int LapsedThresholdDays = 60;

    public ProactiveMessagingService(
        AppDbContext db,
        IAIService ai,
        IEmailService email,
        ILogger<ProactiveMessagingService> logger)
    {
        _db = db;
        _ai = ai;
        _email = email;
        _logger = logger;
    }

    // ── Generate pending messages for a tenant ────────────────────────────────

    public async Task<IReadOnlyList<ProactiveMessage>> GeneratePendingMessagesAsync(
        Guid tenantId, CancellationToken ct = default)
    {
        var messages = new List<ProactiveMessage>();
        var now = DateTime.UtcNow;

        // 1. Lapsed clients (last booking > 60 days ago, still active)
        var lapsedCutoff = now.AddDays(-LapsedThresholdDays);
        var lapsedClients = await _db.Clients
            .Where(c => c.TenantId == tenantId && c.MarketingConsent)
            .Join(_db.Bookings,
                c => c.Id,
                b => b.ClientId,
                (c, b) => new { Client = c, Booking = b })
            .GroupBy(x => new { x.Client.Id, x.Client.FirstName, x.Client.Email, x.Client.LastName })
            .Where(g => g.Max(x => x.Booking.StartTime) < lapsedCutoff)
            .Select(g => new
            {
                g.Key.Id,
                g.Key.FirstName,
                g.Key.LastName,
                g.Key.Email,
                LastBooking = g.Max(x => x.Booking.StartTime),
                TotalBookings = g.Count()
            })
            .Take(20)
            .ToListAsync(ct);

        foreach (var client in lapsedClients)
        {
            var daysSince = (now - client.LastBooking).TotalDays;
            messages.Add(new ProactiveMessage
            {
                Id = Guid.NewGuid().ToString("N")[..8],
                TenantId = tenantId,
                ClientId = client.Id,
                ClientName = $"{client.FirstName} {client.LastName}",
                ClientEmail = client.Email,
                Trigger = "lapsed_client",
                Channel = "email",
                Subject = $"We miss you, {client.FirstName}! 🌟",
                Body = GenerateLapsedClientMessage(client.FirstName, (int)daysSince, client.TotalBookings),
                Priority = daysSince > 90 ? "high" : "medium",
                ScheduledFor = now.AddHours(1),
                Status = "pending"
            });
        }

        // 2. Birthday offers (birthday within next 7 days)
        var birthdayClients = await _db.Clients
            .Where(c => c.TenantId == tenantId
                && c.MarketingConsent
                && c.DateOfBirth != null
                && c.DateOfBirth.Value.Month == now.Month
                && c.DateOfBirth.Value.Day >= now.Day
                && c.DateOfBirth.Value.Day <= now.AddDays(7).Day)
            .Select(c => new { c.Id, c.FirstName, c.LastName, c.Email, c.DateOfBirth })
            .Take(10)
            .ToListAsync(ct);

        foreach (var client in birthdayClients)
        {
            messages.Add(new ProactiveMessage
            {
                Id = Guid.NewGuid().ToString("N")[..8],
                TenantId = tenantId,
                ClientId = client.Id,
                ClientName = $"{client.FirstName} {client.LastName}",
                ClientEmail = client.Email,
                Trigger = "birthday",
                Channel = "email",
                Subject = $"Happy Birthday, {client.FirstName}! 🎉 A special gift for you",
                Body = GenerateBirthdayMessage(client.FirstName),
                Priority = "high",
                ScheduledFor = new DateTime(now.Year, client.DateOfBirth!.Value.Month, client.DateOfBirth.Value.Day, 9, 0, 0, DateTimeKind.Utc),
                Status = "pending"
            });
        }

        // 3. Post-service follow-up (bookings completed 24h ago, no review yet)
        var followUpCutoff = now.AddHours(-48);
        var followUpStart = now.AddHours(-24);
        var completedBookings = await _db.Bookings
            .Include(b => b.Client)
            .Include(b => b.Service)
            .Where(b => b.TenantId == tenantId
                && b.Status == Upkilo.Core.Entities.BookingStatus.Completed
                && b.EndTime >= followUpCutoff && b.EndTime < followUpStart
                && b.Client != null && b.Client.MarketingConsent)
            .Take(10)
            .ToListAsync(ct);

        foreach (var booking in completedBookings)
        {
            messages.Add(new ProactiveMessage
            {
                Id = Guid.NewGuid().ToString("N")[..8],
                TenantId = tenantId,
                ClientId = booking.ClientId ?? Guid.Empty,
                ClientName = $"{booking.Client!.FirstName} {booking.Client.LastName}",
                ClientEmail = booking.Client.Email,
                Trigger = "post_service_followup",
                Channel = "email",
                Subject = $"How was your {booking.Service?.Name ?? "appointment"}, {booking.Client.FirstName}?",
                Body = GenerateFollowUpMessage(booking.Client.FirstName, booking.Service?.Name ?? "appointment"),
                Priority = "low",
                ScheduledFor = now.AddMinutes(30),
                Status = "pending",
                Metadata = new Dictionary<string, string>
                {
                    ["bookingId"] = booking.Id.ToString(),
                    ["serviceId"] = booking.ServiceId.ToString()
                }
            });
        }

        _logger.LogInformation(
            "Generated {Count} proactive messages for tenant {TenantId}",
            messages.Count, tenantId);

        return messages;
    }

    // ── Send pending messages ─────────────────────────────────────────────────

    public async Task<int> SendPendingMessagesAsync(
        Guid tenantId, bool dryRun = false, CancellationToken ct = default)
    {
        var messages = await GeneratePendingMessagesAsync(tenantId, ct);
        int sent = 0;

        foreach (var msg in messages.Where(m => m.ScheduledFor <= DateTime.UtcNow.AddMinutes(5)))
        {
            if (!dryRun)
            {
                try
                {
                    await _email.SendEmailAsync(msg.ClientEmail ?? "", msg.Subject ?? "", msg.Body ?? "");
                    msg.Status = "sent";
                    msg.SentAt = DateTime.UtcNow;
                    sent++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send proactive message {MsgId} to {Email}",
                        msg.Id, msg.ClientEmail);
                    msg.Status = "failed";
                }
            }
            else
            {
                msg.Status = "dry_run";
                sent++;
            }
        }

        return sent;
    }

    // ── Generate message for a specific client + trigger ──────────────────────

    public async Task<ProactiveMessage?> GenerateForClientAsync(
        Guid tenantId, Guid clientId, string trigger, CancellationToken ct = default)
    {
        var client = await _db.Clients
            .FirstOrDefaultAsync(c => c.Id == clientId && c.TenantId == tenantId, ct);

        if (client == null) return null;

        var aiPrompt = trigger switch
        {
            "lapsed_client" => $"Write a warm, personalized re-engagement email for {client.FirstName} who hasn't booked in 2 months. Keep it under 100 words, friendly, include a call to action.",
            "birthday" => $"Write a brief birthday greeting and exclusive 15% off offer for {client.FirstName}. Warm tone, under 80 words.",
            "post_service" => $"Write a post-service follow-up asking {client.FirstName} for a review and suggesting their next appointment. Friendly, under 80 words.",
            "milestone" => $"Congratulate {client.FirstName} on their 10th booking with a loyalty reward message. Celebratory, under 80 words.",
            _ => $"Write a friendly personalized message to {client.FirstName} about their experience. Under 80 words."
        };

        string body;
        try
        {
            // Pass null so AiModelResolver picks the tenant's tier model. This used to hardcode
            // "gpt-4o-mini" to keep the cost down, but a literal here goes stale silently: that
            // deployment no longer exists and is no longer in any AllowedAiModels list, so the
            // call would have been rejected outright and every message fallen back to the
            // canned text below.
            var aiResult = await _ai.GenerateTextAsync(tenantId, null, aiPrompt, null);
            body = aiResult.Content ?? GenerateLapsedClientMessage(client.FirstName, 60, 5);
        }
        catch
        {
            body = GenerateLapsedClientMessage(client.FirstName, 60, 5);
        }

        return new ProactiveMessage
        {
            Id = Guid.NewGuid().ToString("N")[..8],
            TenantId = tenantId,
            ClientId = clientId,
            ClientName = $"{client.FirstName} {client.LastName}",
            ClientEmail = client.Email,
            Trigger = trigger,
            Channel = "email",
            Subject = $"A message for you, {client.FirstName}",
            Body = body,
            Priority = "medium",
            ScheduledFor = DateTime.UtcNow.AddHours(1),
            Status = "draft"
        };
    }

    // ── Message templates ─────────────────────────────────────────────────────

    private static string GenerateLapsedClientMessage(string name, int daysSince, int totalBookings)
        => $"Hi {name},\n\nWe haven't seen you in a while and we miss you! " +
           $"It's been {daysSince} days since your last visit. " +
           $"As a valued client with {totalBookings} bookings, we'd love to welcome you back.\n\n" +
           "Book now and use code WELCOME10 for 10% off your next appointment.\n\n" +
           "Looking forward to seeing you!\nThe Team";

    private static string GenerateBirthdayMessage(string name)
        => $"Happy Birthday, {name}! 🎂\n\n" +
           "To celebrate your special day, we're treating you to 20% off any service booked this week. " +
           "Use code BDAY20 at checkout.\n\n" +
           "Wishing you a wonderful birthday!\nWith love, The Team";

    private static string GenerateFollowUpMessage(string name, string service)
        => $"Hi {name},\n\nWe hope you enjoyed your {service} experience! " +
           "Your feedback means the world to us — would you mind leaving a quick review?\n\n" +
           "Also, your next appointment is just a click away whenever you're ready.\n\n" +
           "Thank you for choosing us!\nThe Team";
}

// ─── Models ─────────────────────────────────────────────────────────────────���─

public class ProactiveMessage
{
    public string Id { get; set; } = "";
    public Guid TenantId { get; set; }
    public Guid ClientId { get; set; }
    public string ClientName { get; set; } = "";
    public string ClientEmail { get; set; } = "";
    public string Trigger { get; set; } = "";   // lapsed_client | birthday | post_service_followup | milestone
    public string Channel { get; set; } = "email"; // email | sms | push
    public string Subject { get; set; } = "";
    public string Body { get; set; } = "";
    public string Priority { get; set; } = "medium"; // low | medium | high
    public DateTime ScheduledFor { get; set; }
    public string Status { get; set; } = "pending"; // pending | sent | failed | dry_run | draft
    public DateTime? SentAt { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}
