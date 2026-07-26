using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Jobs;

public class GdprDataDeletionJob
{
    private readonly AppDbContext _context;
    private readonly ILogger<GdprDataDeletionJob> _logger;

    public GdprDataDeletionJob(AppDbContext context, ILogger<GdprDataDeletionJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task ExecuteAsync(Guid clientId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting GDPR data deletion for Client {ClientId}", clientId);

        var client = await _context.Clients.FindAsync(new object[] { clientId }, cancellationToken);
        if (client == null)
        {
            _logger.LogWarning("Client {ClientId} not found for GDPR deletion.", clientId);
            return;
        }

        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // Nullify PII instead of hard deleting records to maintain financial integrity
            // (Standard practice is to anonymize rather than DROP if financial records rely on the ID)
            client.FirstName = "Anonymized";
            client.LastName = "User";
            client.Email = $"deleted_{Guid.NewGuid()}@anonymized.local";
            client.Phone = null;
            client.AddressLine1 = null;
            client.AddressLine2 = null;
            client.City = null;
            client.State = null;
            client.PostalCode = null;
            client.DateOfBirth = null;

            // Delete non-critical related PII models
            var photos = await _context.ClientPhotos.Where(p => p.ClientId == clientId).ToListAsync(cancellationToken);
            _context.ClientPhotos.RemoveRange(photos);
            
            var waitlists = await _context.WaitlistEntries.Where(w => w.ClientId == clientId).ToListAsync(cancellationToken);
            _context.WaitlistEntries.RemoveRange(waitlists);

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("GDPR anonymization complete for Client {ClientId}", clientId);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Failed to execute GDPR deletion for Client {ClientId}", clientId);
            throw;
        }
    }
}
