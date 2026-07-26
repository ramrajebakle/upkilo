using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Background;

/// <summary>
/// Background job that runs daily to enforce GDPR "Right to be Forgotten" mandates.
/// Finds soft-deleted clients or users with revoked consent past the 30-day retention window
/// and permanently anonymizes their PII (Personally Identifiable Information).
/// </summary>
public class GdprDataScrubberJob
{
    private readonly AppDbContext _context;
    private readonly ILogger<GdprDataScrubberJob> _logger;

    public GdprDataScrubberJob(AppDbContext context, ILogger<GdprDataScrubberJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        _logger.LogInformation("Starting GDPR Data Scrubber Job...");
        var cutoffDate = DateTime.UtcNow.AddDays(-30);

        // 1. Find Clients that were soft-deleted over 30 days ago
        // Must bypass global query filters to see soft-deleted records
        var clientsToScrub = await _context.Clients
            .IgnoreQueryFilters()
            .Where(c => c.IsDeleted && c.DeletedAt <= cutoffDate)
            .ToListAsync();

        var scrubCount = 0;

        foreach (var client in clientsToScrub)
        {
            // Scramble PII with cryptographic hashes or generic redaction strings
            client.FirstName = "Redacted";
            client.LastName = "Redacted";
            client.Email = $"redacted_{Guid.NewGuid()}@domain.invalid";
            client.Phone = "+00000000000";
            client.AddressLine1 = "Redacted";
            client.AddressLine2 = null;
            client.City = "Redacted";
            client.PostalCode = "00000";
            client.DateOfBirth = null;
            client.AvatarUrl = null;
            client.Gender = "Redacted";

            // Mark as scrubbed
            client.UpdatedAt = DateTime.UtcNow;
            
            scrubCount++;
        }

        // 2. Find revoked GDPR consents past 30 days
        var consentsToScrub = await _context.GdprConsents
            .Where(g => g.IsGranted == false && g.ProcessedAt <= cutoffDate)
            .ToListAsync();

        foreach (var consent in consentsToScrub)
        {
            consent.IpAddress = "127.0.0.1"; // Redact IP
            consent.UserAgent = "Redacted";
            consent.UpdatedAt = DateTime.UtcNow;
        }

        if (scrubCount > 0 || consentsToScrub.Any())
        {
            await _context.SaveChangesAsync();
            _logger.LogWarning("GDPR Scrubber Job anonymized {ClientCount} clients and {ConsentCount} consent records.", 
                scrubCount, consentsToScrub.Count);
        }
        else
        {
            _logger.LogInformation("GDPR Scrubber Job completed. No records met the 30-day retention cutoff.");
        }
    }
}
