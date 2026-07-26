using Microsoft.Extensions.Logging;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Upkilo.Infrastructure.Services
{
    public interface IGdprAutomationService
    {
        Task<bool> AnonymizeTenantDataAsync(Guid tenantId);
        Task<bool> AnonymizeClientDataAsync(Guid clientId);
        Task<string> ExportClientDataAsync(Guid clientId);
    }

    public class GdprAutomationService : IGdprAutomationService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<GdprAutomationService> _logger;

        public GdprAutomationService(AppDbContext context, ILogger<GdprAutomationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> AnonymizeClientDataAsync(Guid clientId)
        {
            _logger.LogInformation("GDPR: Anonymizing client {ClientId}", clientId);
            
            var client = await _context.Clients.FindAsync(clientId);
            if (client == null) return false;

            // Scrub PII
            client.FirstName = "Anonymized";
            client.LastName = "User";
            client.Email = $"deleted_{Guid.NewGuid():N}@upkilo.internal";
            client.PhoneNumber = "0000000000";
            client.Address = null;
            client.IsActive = false;

            await _context.SaveChangesAsync();
            _logger.LogInformation("GDPR: Client {ClientId} anonymized successfully", clientId);
            return true;
        }

        public async Task<bool> AnonymizeTenantDataAsync(Guid tenantId)
        {
            _logger.LogInformation("GDPR: Bulk anonymizing all clients for tenant {TenantId}", tenantId);
            
            var clients = await _context.Clients.Where(c => c.TenantId == tenantId).ToListAsync();
            foreach (var client in clients)
            {
                await AnonymizeClientDataAsync(client.Id);
            }

            return true;
        }

        public async Task<string> ExportClientDataAsync(Guid clientId)
        {
            _logger.LogInformation("GDPR: Exporting data for client {ClientId}", clientId);
            
            var clientData = await _context.Clients
                .Where(c => c.Id == clientId)
                .Include(c => c.DetailedNotes)
                .Select(c => new
                {
                    c.FirstName,
                    c.LastName,
                    c.Email,
                    c.CreatedAt,
                    Notes = c.DetailedNotes.Select(n => new { n.Content, n.CreatedAt }),
                    BookingsCount = _context.Bookings.Count(b => b.ClientId == c.Id)
                })
                .FirstOrDefaultAsync();

            return JsonSerializer.Serialize(clientData, new JsonSerializerOptions { WriteIndented = true });
        }
    }
}
