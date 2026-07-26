using Microsoft.Extensions.Logging;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Upkilo.Infrastructure.Services
{
    public interface IComplianceEvidenceService
    {
        Task CollectEvidenceAsync(Guid tenantId, string controlId, string category, string description, string evidenceType, string? evidenceUrl = null);
        Task<IEnumerable<Soc2Evidence>> GetEvidenceHistoryAsync(Guid tenantId, string? category = null);
        Task<HipaaConfig> GetHipaaConfigAsync(Guid tenantId);
        Task UpdateHipaaConfigAsync(Guid tenantId, HipaaConfig config);
    }

    public class ComplianceEvidenceService : IComplianceEvidenceService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ComplianceEvidenceService> _logger;

        public ComplianceEvidenceService(AppDbContext context, ILogger<ComplianceEvidenceService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task CollectEvidenceAsync(Guid tenantId, string controlId, string category, string description, string evidenceType, string? evidenceUrl = null)
        {
            _logger.LogInformation("Collecting compliance evidence for control {ControlId} in tenant {TenantId}", controlId, tenantId);

            var evidence = new Soc2Evidence
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ControlId = controlId,
                Category = category,
                Description = description,
                EvidenceType = evidenceType,
                EvidenceUrl = evidenceUrl,
                Status = "Compliant",
                CollectedAt = DateTime.UtcNow
            };

            _context.Soc2Evidences.Add(evidence);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Soc2Evidence>> GetEvidenceHistoryAsync(Guid tenantId, string? category = null)
        {
            var query = _context.Soc2Evidences.Where(e => e.TenantId == tenantId);
            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(e => e.Category == category);
            }

            return await query.OrderByDescending(e => e.CollectedAt).ToListAsync();
        }

        public async Task<HipaaConfig> GetHipaaConfigAsync(Guid tenantId)
        {
            var config = await _context.HipaaConfigs.FirstOrDefaultAsync(c => c.TenantId == tenantId);
            if (config == null)
            {
                config = new HipaaConfig
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    IsEnabled = false,
                    EncryptionAtRest = true,
                    EncryptionInTransit = true,
                    AccessLogging = true
                };
                _context.HipaaConfigs.Add(config);
                await _context.SaveChangesAsync();
            }
            return config;
        }

        public async Task UpdateHipaaConfigAsync(Guid tenantId, HipaaConfig config)
        {
            var existing = await _context.HipaaConfigs.FirstOrDefaultAsync(c => c.TenantId == tenantId);
            if (existing != null)
            {
                existing.IsEnabled = config.IsEnabled;
                existing.BaaDocument = config.BaaDocument;
                existing.LastAuditAt = DateTime.UtcNow;
                _context.HipaaConfigs.Update(existing);
            }
            else
            {
                config.TenantId = tenantId;
                _context.HipaaConfigs.Add(config);
            }
            await _context.SaveChangesAsync();
        }
    }
}
