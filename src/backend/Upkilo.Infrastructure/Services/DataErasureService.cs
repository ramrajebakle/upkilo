using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// Service for implementing GDPR/DPDP "Right to Erasure" (Right to be Forgotten).
/// Handles permanent deletion or anonymization of user and client data across ALL stores,
/// including AI conversations, usage logs, and audit entries.
/// </summary>
public class DataErasureService
{
    private readonly AppDbContext _context;
    private readonly IPiiScrubberService _piiScrubber;
    private readonly ILogger<DataErasureService> _logger;

    public DataErasureService(AppDbContext context, IPiiScrubberService piiScrubber, ILogger<DataErasureService> logger)
    {
        _context = context;
        _piiScrubber = piiScrubber;
        _logger = logger;
    }

    public async Task EraseUserAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return;

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // 1. Delete all active sessions
            var sessions = _context.UserSessions.Where(s => s.UserId == userId);
            _context.UserSessions.RemoveRange(sessions);

            // 2. Delete AI conversations and messages (contain PII in message content).
            // Conversations are linked to Client records (not User records directly).
            // Find the client whose email matches this user's email, then delete their conversations.
            var linkedClientId = await _context.Clients
                .Where(c => c.Email == user.Email && c.TenantId == user.TenantId)
                .Select(c => (Guid?)c.Id)
                .FirstOrDefaultAsync();

            var conversationIds = linkedClientId.HasValue
                ? await _context.AIConversations
                    .Where(c => c.TenantId == user.TenantId && c.ClientId == linkedClientId)
                    .Select(c => c.Id)
                    .ToListAsync()
                : new List<Guid>();

            if (conversationIds.Any())
            {
                var messages = _context.AIMessages
                    .Where(m => conversationIds.Contains(m.ConversationId));
                _context.AIMessages.RemoveRange(messages);

                var conversations = _context.AIConversations
                    .Where(c => conversationIds.Contains(c.Id));
                _context.AIConversations.RemoveRange(conversations);
            }

            // 3. Delete AI usage logs linked to this user
            var usageLogs = _context.AIUsageLogs
                .Where(l => l.UserId == userId);
            _context.AIUsageLogs.RemoveRange(usageLogs);

            // 4. Redact audit log entries — keep for legal compliance but scrub PII fields
            var auditEntries = await _context.AuditEntries
                .Where(a => a.UserId == userId)
                .ToListAsync();
            foreach (var entry in auditEntries)
            {
                entry.OldValues = null;
                entry.NewValues = null;
                entry.IpAddress = "REDACTED";
                entry.UserAgent = "REDACTED";
            }

            // 5. Anonymize bookings (preserve for financial/tax records, strip identity)
            var bookings = await _context.Bookings
                .Where(b => b.CustomerId == userId.ToString() || b.CustomerEmail == user.Email)
                .ToListAsync();

            foreach (var booking in bookings)
            {
                booking.CustomerName  = "ANONYMIZED";
                booking.CustomerEmail = "anonymized@upkilo.com";
                booking.CustomerPhone = "ANONYMIZED";
                booking.Notes         = _piiScrubber.Scrub(booking.Notes ?? string.Empty);
            }

            // 6. Anonymize client record if user is linked to one
            var client = await _context.Clients
                .FirstOrDefaultAsync(c => c.Email == user.Email && c.TenantId == user.TenantId);
            if (client != null)
            {
                client.FirstName = "ANONYMIZED";
                client.LastName  = "USER";
                client.Email     = $"erased_{userId:N}@upkilo.com";
                client.Phone     = "ANONYMIZED";
                client.Notes     = null;
                client.Tags      = null;
            }

            // 7. Remove the user record itself
            _context.Users.Remove(user);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation(
                "User {UserId} data fully erased per GDPR/DPDP request. " +
                "Anonymized {BookingCount} bookings, deleted {ConvCount} AI conversations, " +
                "redacted {AuditCount} audit entries.",
                userId, bookings.Count, conversationIds.Count, auditEntries.Count);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to erase data for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Purges AI conversations older than the retention policy for a tenant.
    /// Called by ConversationPurgeJob (Hangfire daily job).
    /// </summary>
    public async Task PurgeExpiredAIConversationsAsync(Guid tenantId, int retentionDays = 90, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-retentionDays);
        const int batchSize = 500;
        int totalPurged = 0;

        while (true)
        {
            // Batch to avoid loading all expired conversations into memory at once.
            var expiredConversations = await _context.AIConversations
                .Where(c => c.TenantId == tenantId && c.CreatedAt < cutoff)
                .Take(batchSize)
                .ToListAsync(cancellationToken);

            if (expiredConversations.Count == 0) break;

            var expiredIds = expiredConversations.Select(c => c.Id).ToList();

            var messages = await _context.AIMessages
                .Where(m => expiredIds.Contains(m.ConversationId))
                .ToListAsync(cancellationToken);

            foreach (var msg in messages)
            {
                msg.Content     = "[Content purged per retention policy]";
                msg.ToolOutputs = null;
                msg.Metadata    = null;
            }

            _context.AIConversations.RemoveRange(expiredConversations);

            _context.Database.SetCommandTimeout(30);
            await _context.SaveChangesAsync(cancellationToken);

            totalPurged += expiredConversations.Count;
        }

        if (totalPurged > 0)
            _logger.LogInformation(
                "Purged {Count} expired AI conversations for tenant {TenantId} (retention: {Days} days)",
                totalPurged, tenantId, retentionDays);
    }
}
