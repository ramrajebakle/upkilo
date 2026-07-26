using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Interfaces;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data;
using Upkilo.Infrastructure.Jobs;
using Upkilo.Infrastructure.Services;

namespace Upkilo.API.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize(Roles = "EnterpriseAdmin,Admin")]
    public class ComplianceController : ControllerBase
    {
        private readonly IComplianceEvidenceService _complianceService;
        private readonly ITenantProvider _tenantProvider;
        private readonly AppDbContext _context;
        private readonly GdprDataDeletionJob _gdprJob;
        private readonly ILogger<ComplianceController> _logger;

        public ComplianceController(
            IComplianceEvidenceService complianceService,
            ITenantProvider tenantProvider,
            AppDbContext context,
            GdprDataDeletionJob gdprJob,
            ILogger<ComplianceController> logger)
        {
            _complianceService = complianceService;
            _tenantProvider = tenantProvider;
            _context = context;
            _gdprJob = gdprJob;
            _logger = logger;
        }

        private Guid GetTenantId() => _tenantProvider.GetTenantId()
            ?? throw new UnauthorizedAccessException("Tenant context not available");

        /// <summary>
        /// Get HIPAA configuration for the tenant
        /// </summary>
        [HttpGet("hipaa")]
        public async Task<IActionResult> GetHipaaConfig()
        {
            var config = await _complianceService.GetHipaaConfigAsync(GetTenantId());
            return Ok(config);
        }

        /// <summary>
        /// Update HIPAA configuration
        /// </summary>
        [HttpPut("hipaa")]
        public async Task<IActionResult> UpdateHipaaConfig([FromBody] HipaaConfig config)
        {
            await _complianceService.UpdateHipaaConfigAsync(GetTenantId(), config);
            return Ok(new { message = "HIPAA configuration updated" });
        }

        /// <summary>
        /// Get SOC2 evidence history
        /// </summary>
        [HttpGet("soc2/evidence")]
        public async Task<IActionResult> GetSoc2Evidence([FromQuery] string? category = null)
        {
            var evidence = await _complianceService.GetEvidenceHistoryAsync(GetTenantId(), category);
            return Ok(evidence);
        }

        /// <summary>
        /// Manually trigger evidence collection
        /// </summary>
        [HttpPost("soc2/collect")]
        public async Task<IActionResult> CollectEvidence([FromBody] CollectEvidenceRequest request)
        {
            await _complianceService.CollectEvidenceAsync(
                GetTenantId(),
                request.ControlId,
                request.Category,
                request.Description,
                request.EvidenceType,
                request.EvidenceUrl);

            return Ok(new { message = "Evidence collected successfully" });
        }

        // ── C1: GDPR Right-to-Erasure ─────────────────────────────────────────

        /// <summary>
        /// C1: Request GDPR right-to-erasure for a client. Anonymizes PII within 30 days (immediately in this impl).
        /// Per GDPR Art. 17 — right to erasure / right to be forgotten.
        /// </summary>
        [HttpPost("gdpr/erasure")]
        public async Task<IActionResult> RequestErasure([FromBody] GdprErasureRequest request)
        {
            var tenantId = GetTenantId();

            var client = await _context.Clients
                .FirstOrDefaultAsync(c => c.Id == request.ClientId && c.TenantId == tenantId);

            if (client == null)
                return NotFound(new { message = "Client not found." });

            _logger.LogInformation("[GDPR] Erasure requested for client {ClientId} in tenant {TenantId} by {UserId}",
                request.ClientId, tenantId, (User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value));

            // Execute anonymization immediately (can be queued to Hangfire in prod for audit trail)
            await _gdprJob.ExecuteAsync(request.ClientId, CancellationToken.None);

            return Ok(new
            {
                requestId = Guid.NewGuid(),
                clientId = request.ClientId,
                status = "completed",
                processedAt = DateTime.UtcNow,
                message = "Client PII has been anonymized in compliance with GDPR Art. 17.",
                dataRetained = new[]
                {
                    "Anonymized booking history (financial integrity)",
                    "Anonymized payment records (7-year legal retention)",
                    "Audit logs (compliance requirement)"
                },
                dataDeleted = new[]
                {
                    "Full name → 'Anonymized User'",
                    "Email → anonymized placeholder",
                    "Phone, address, date of birth",
                    "Client photos",
                    "Waitlist entries"
                }
            });
        }

        /// <summary>
        /// C1: List pending/completed GDPR erasure requests (admin audit view).
        /// </summary>
        [HttpGet("gdpr/erasure")]
        public async Task<IActionResult> ListErasureRequests()
        {
            var tenantId = GetTenantId();

            // Return audit entries tagged as GDPR erasure
            var erasures = await _context.AuditEntries
                .Where(e => e.TenantId == tenantId && e.Action == "GdprErasure")
                .OrderByDescending(e => e.PerformedAt)
                .Take(100)
                .Select(e => new { e.Id, e.EntityId, e.PerformedAt, performedBy = e.UserName, e.Action })
                .ToListAsync();

            return Ok(new { erasures, total = erasures.Count });
        }
    }

    public class CollectEvidenceRequest
    {
        public string ControlId { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string EvidenceType { get; set; } = "Manual";
        public string? EvidenceUrl { get; set; }
    }

    public class GdprErasureRequest
    {
        public Guid ClientId { get; set; }
        public string? Reason { get; set; }
    }
}
