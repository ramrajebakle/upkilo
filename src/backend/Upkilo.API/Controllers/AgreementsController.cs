using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Upkilo.API.Middleware;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Controllers;

/// <summary>
/// Platform-admin management of the legal agreements Upkilo holds with each tenant:
/// the HIPAA BAA and the uptime SLA.
///
/// These were previously unmanageable. The BAA could be signed through
/// /consent/hipaa-baa/sign, but nothing in the product ever displayed who had
/// signed, on which document version, or which tenants had not signed at all —
/// and the SLA had no representation anywhere. This is the platform team's view
/// of both.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/agreements")]
[Authorize(Roles = "SuperAdmin")]
public class AgreementsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<AgreementsController> _logger;

    public AgreementsController(AppDbContext context, ILogger<AgreementsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// GET — every tenant with its BAA and SLA state, including tenants that have
    /// neither. Listing the gaps is the point: a compliance view that only shows
    /// signed agreements cannot tell you who still owes one.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? status = null)
    {
        // IgnoreQueryFilters is deliberate. TenantAgreement is a TenantEntity, so the
        // global filter scopes it to the resolved tenant; a platform-wide view must
        // cross that boundary explicitly rather than depend on no tenant happening to
        // be resolved from the request host.
        var agreements = await _context.TenantAgreements
            .IgnoreQueryFilters()
            .Where(a => !a.IsDeleted)
            .ToListAsync();

        var tenants = await _context.Tenants
            .IgnoreQueryFilters()
            .Where(t => !t.IsDeleted)
            .Select(t => new { t.Id, t.Name })
            .ToListAsync();

        var rows = tenants.Select(t =>
        {
            var baa = agreements.FirstOrDefault(a => a.TenantId == t.Id && a.Type == AgreementType.HipaaBaa);
            var sla = agreements.FirstOrDefault(a => a.TenantId == t.Id && a.Type == AgreementType.Sla);
            return new
            {
                tenantId = t.Id,
                tenantName = t.Name,
                baa = Describe(baa),
                sla = sla == null ? null : new
                {
                    status = sla.Status.ToString(),
                    uptimeTargetPercent = sla.UptimeTargetPercent,
                    effectiveFrom = sla.EffectiveFrom,
                    expiresAt = sla.ExpiresAt,
                    notes = sla.Notes
                }
            };
        });

        if (!string.IsNullOrWhiteSpace(status))
        {
            rows = status.Equals("missing-baa", StringComparison.OrdinalIgnoreCase)
                ? rows.Where(r => r.baa == null || r.baa.status != nameof(AgreementStatus.Signed))
                : rows;
        }

        var list = rows.ToList();
        return Ok(new { data = list, total = list.Count });
    }

    private static dynamic? Describe(TenantAgreement? a) => a == null ? null : new
    {
        status = a.Status.ToString(),
        documentVersion = a.DocumentVersion,
        signatoryName = a.SignatoryName,
        signatoryTitle = a.SignatoryTitle,
        signedAt = a.SignedAt,
        signedFromIp = a.SignedFromIp,
        effectiveFrom = a.EffectiveFrom,
        expiresAt = a.ExpiresAt,
        notes = a.Notes
    };

    /// <summary>
    /// PUT — record or amend an agreement for one tenant.
    ///
    /// Deliberately upsert-by-(tenant, type): a tenant holds one current BAA and one
    /// current SLA, and letting the platform team create a second row for the same
    /// pair would make "is this tenant covered?" ambiguous at exactly the moment it
    /// matters.
    /// </summary>
    [HttpPut("{tenantId:guid}/{type}")]
    public async Task<IActionResult> Upsert(Guid tenantId, string type, [FromBody] AgreementUpsertRequest request)
    {
        if (!Enum.TryParse<AgreementType>(type, true, out var agreementType))
            return BadRequest(ApiResponse.Fail($"Type must be one of: {string.Join(", ", Enum.GetNames<AgreementType>())}"));

        if (!Enum.TryParse<AgreementStatus>(request.Status, true, out var status))
            return BadRequest(ApiResponse.Fail($"Status must be one of: {string.Join(", ", Enum.GetNames<AgreementStatus>())}"));

        var tenantExists = await _context.Tenants.IgnoreQueryFilters().AnyAsync(t => t.Id == tenantId && !t.IsDeleted);
        if (!tenantExists) return NotFound(ApiResponse.Fail("Tenant not found"));

        // A signed agreement without a signatory is the gap this table exists to close,
        // so it is rejected rather than stored half-complete.
        if (status == AgreementStatus.Signed && string.IsNullOrWhiteSpace(request.SignatoryName))
            return BadRequest(ApiResponse.Fail("signatoryName is required when status is Signed"));

        if (agreementType == AgreementType.Sla &&
            request.UptimeTargetPercent is { } target &&
            (target <= 0 || target > 100))
            return BadRequest(ApiResponse.Fail("uptimeTargetPercent must be between 0 and 100"));

        var agreement = await _context.TenantAgreements
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(a => a.TenantId == tenantId && a.Type == agreementType && !a.IsDeleted);

        if (agreement == null)
        {
            agreement = new TenantAgreement { TenantId = tenantId, Type = agreementType };
            _context.TenantAgreements.Add(agreement);
        }

        agreement.Status = status;
        agreement.DocumentVersion = request.DocumentVersion;
        agreement.SignatoryName = request.SignatoryName;
        agreement.SignatoryTitle = request.SignatoryTitle;
        agreement.EffectiveFrom = request.EffectiveFrom;
        agreement.ExpiresAt = request.ExpiresAt;
        agreement.UptimeTargetPercent = agreementType == AgreementType.Sla ? request.UptimeTargetPercent : null;
        agreement.Notes = request.Notes;
        agreement.UpdatedAt = DateTime.UtcNow;
        if (status == AgreementStatus.Signed && agreement.SignedAt == null)
            agreement.SignedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("[Agreements] {Type} for tenant {TenantId} set to {Status} by platform admin",
            agreementType, tenantId, status);

        return Ok(ApiResponse<object>.Ok(new { tenantId, type = agreementType.ToString(), status = status.ToString() }));
    }
}

public class AgreementUpsertRequest
{
    public string Status { get; set; } = string.Empty;
    public string? DocumentVersion { get; set; }
    public string? SignatoryName { get; set; }
    public string? SignatoryTitle { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public decimal? UptimeTargetPercent { get; set; }
    public string? Notes { get; set; }
}
