using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/consent")]
[Authorize]
public class ConsentController : ControllerBase
{
    private readonly IConsentService _consentService;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<ConsentController> _logger;
    private readonly AppDbContext _context;

    public ConsentController(IConsentService consentService, ITenantProvider tenantProvider, ILogger<ConsentController> logger, AppDbContext context)
    {
        _consentService = consentService;
        _tenantProvider = tenantProvider;
        _logger = logger;
        _context = context;
    }

    private Guid GetTenantId() => _tenantProvider.GetTenantId() ?? Guid.Empty;
    private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
    private string GetIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";

    /// <summary>
    /// Record client consent (e.g. Terms, Marketing)
    /// </summary>
    [HttpPost("client/{clientId}/record")]
    public async Task<IActionResult> RecordConsent(Guid clientId, [FromBody] ConsentRecordRequest request)
    {
        var tenantId = GetTenantId();
        var ip = GetIpAddress();
        var success = await _consentService.RecordConsentAsync(tenantId, clientId, request.ConsentType, request.Granted, ip);

        if (!success) return BadRequest(new { error = "Failed to record consent" });

        return Ok(new { success = true });
    }

    /// <summary>
    /// Revoke specific consent
    /// </summary>
    [HttpPost("client/{clientId}/revoke")]
    public async Task<IActionResult> RevokeConsent(Guid clientId, [FromBody] RevokeConsentRequest request)
    {
        var tenantId = GetTenantId();
        var success = await _consentService.RevokeConsentAsync(tenantId, clientId, request.ConsentType);

        if (!success) return BadRequest(new { error = "Failed to revoke consent or consent not found" });

        return Ok(new { success = true });
    }

    /// <summary>
    /// Get all recorded consents for a client
    /// </summary>
    [HttpGet("client/{clientId}")]
    public async Task<IActionResult> GetAllConsents(Guid clientId)
    {
        var tenantId = GetTenantId();
        var consents = await _consentService.GetAllConsentsAsync(tenantId, clientId);
        return Ok(new { data = consents });
    }

    /// <summary>
    /// Get status of specific consent
    /// </summary>
    [HttpGet("client/{clientId}/status")]
    public async Task<IActionResult> GetConsentStatus(Guid clientId, [FromQuery] string type)
    {
        if (string.IsNullOrEmpty(type)) return BadRequest("type query parameter is required");

        var tenantId = GetTenantId();
        var status = await _consentService.GetConsentStatusAsync(tenantId, clientId, type);
        return Ok(new { consentType = type, status = status.ToString() });
    }

    // ─── C2: HIPAA Business Associate Agreement (BAA) ────────────────────────────

    /// <summary>
    /// C2: GET /consent/hipaa-baa/status — Check if tenant has signed the HIPAA BAA.
    /// Required before medical/dental vertical features are unlocked.
    /// </summary>
    [HttpGet("hipaa-baa/status")]
    public async Task<IActionResult> GetHipaaBaaStatus()
    {
        var tenantId = GetTenantId();
        var consent = await _consentService.GetConsentStatusAsync(tenantId, Guid.Empty, "HIPAA_BAA");
        var signed = consent.ToString() == "Granted";
        return Ok(new
        {
            signed,
            status = signed ? "signed" : "not_signed",
            requiredFor = new[] { "medical_spa", "dental", "healthcare" },
            note = signed ? null : "Sign the BAA to unlock HIPAA-compliant features."
        });
    }

    /// <summary>
    /// C2: GET /consent/hipaa-baa/document — Returns the current BAA template with versioning.
    /// </summary>
    [HttpGet("hipaa-baa/document")]
    [AllowAnonymous]
    public IActionResult GetHipaaBaaDocument()
    {
        return Ok(new
        {
            version = "2024.1",
            effectiveDate = "2024-01-01",
            title = "HIPAA Business Associate Agreement",
            parties = new
            {
                coveredEntity = "The healthcare provider (Tenant) using the Upkilo platform",
                businessAssociate = "Upkilo Inc., a technology company providing practice management software"
            },
            sections = new[]
            {
                new { id = "1", title = "Definitions", summary = "Terms consistent with 45 CFR Parts 160 and 164." },
                new { id = "2", title = "Obligations of Business Associate", summary = "Upkilo will: (a) use PHI only as permitted; (b) implement safeguards per 45 CFR § 164.308, 164.310, 164.312; (c) report breaches within 60 days; (d) make PHI available to HHS; (e) return or destroy PHI on termination." },
                new { id = "3", title = "Permitted Uses and Disclosures", summary = "PHI may be used only to provide services under the Master Agreement, for proper management, and as required by law." },
                new { id = "4", title = "Term and Termination", summary = "Term matches Master Agreement. Either party may terminate if the other breaches a material BAA term and fails to cure within 30 days." },
                // The SOC 2 Type II assertion was removed here. It is a third-party audit
                // with a report a counterparty can demand, Upkilo has not had that audit,
                // and this text sits inside a BAA — a document a covered entity relies on
                // when deciding it may lawfully disclose PHI. The same claim was already
                // struck from the public enterprise page for the same reason.
                // The encryption statements stay: both are implemented.
                new { id = "5", title = "Miscellaneous", summary = "Governed by HIPAA and HITECH. Upkilo encrypts PHI at rest (AES-256) and in transit (TLS 1.3)." }
            },
            signatureRequired = new[] { "tenantName", "authorizedSignatoryName", "authorizedSignatoryTitle", "signatureDate" }
        });
    }

    /// <summary>
    /// C2: POST /consent/hipaa-baa/sign — Digitally sign the HIPAA BAA.
    /// Records a legally binding electronic signature with IP, timestamp, and version.
    /// After signing, medical/dental vertical features are enabled for this tenant.
    /// </summary>
    [HttpPost("hipaa-baa/sign")]
    public async Task<IActionResult> SignHipaaBaa([FromBody] HipaaBaaSignRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.AuthorizedSignatoryName))
            return BadRequest(new { error = "authorizedSignatoryName is required" });
        if (string.IsNullOrWhiteSpace(request.AuthorizedSignatoryTitle))
            return BadRequest(new { error = "authorizedSignatoryTitle is required" });
        if (!request.AgreesToTerms)
            return BadRequest(new { error = "agreesToTerms must be true to proceed" });

        var tenantId = GetTenantId();
        var userId = GetUserId();
        var ip = GetIpAddress();

        // Still written as a GdprConsent row, because VerticalsController's feature
        // gate queries exactly that. Changing the gate and the storage in one step
        // would risk locking medical tenants out of features they had already unlocked.
        var success = await _consentService.RecordConsentAsync(tenantId, userId, "HIPAA_BAA", true, ip);
        if (!success) return StatusCode(500, new { error = "Failed to record HIPAA BAA signature" });

        // The signature block itself. RecordConsentAsync takes no signatory arguments,
        // so before this the name and title were validated above, echoed back in the
        // response, written to the log line below — and then lost. A BAA that cannot
        // say who bound the entity, or in what capacity, cannot do the job a BAA exists
        // to do.
        var version = request.BaaVersion ?? "2024.1";
        var agreement = await _context.TenantAgreements
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(a => a.TenantId == tenantId && a.Type == AgreementType.HipaaBaa && !a.IsDeleted);

        if (agreement == null)
        {
            agreement = new TenantAgreement { TenantId = tenantId, Type = AgreementType.HipaaBaa };
            _context.TenantAgreements.Add(agreement);
        }

        agreement.Status = AgreementStatus.Signed;
        agreement.DocumentVersion = version;
        agreement.SignatoryName = request.AuthorizedSignatoryName;
        agreement.SignatoryTitle = request.AuthorizedSignatoryTitle;
        agreement.SignedAt = DateTime.UtcNow;
        agreement.SignedFromIp = ip;
        agreement.UserAgent = Request.Headers.UserAgent.ToString();
        agreement.EffectiveFrom ??= DateTime.UtcNow;
        agreement.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("[C2] HIPAA BAA signed: tenant={TenantId} signatory={Name} title={Title} ip={IP} version={Version}",
            tenantId, request.AuthorizedSignatoryName, request.AuthorizedSignatoryTitle, ip, request.BaaVersion);

        return Ok(new
        {
            signed = true,
            effectiveDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            baaVersion = request.BaaVersion ?? "2024.1",
            signatoryName = request.AuthorizedSignatoryName,
            signatoryTitle = request.AuthorizedSignatoryTitle,
            ipAddress = ip,
            timestamp = DateTime.UtcNow,
            confirmationNumber = $"BAA-{tenantId:N}"[..16].ToUpper(),
            message = "HIPAA BAA signed. Medical/dental features are now unlocked for your account.",
            featuresUnlocked = new[] { "treatment_plan_templates", "rx_tracking", "insurance_preauth", "hipaa_audit_logs", "phi_encryption_confirmation" }
        });
    }

    /// <summary>
    /// C2: GET /consent/hipaa-baa/latest
    /// </summary>
    [HttpGet("dpa/latest")]
    [AllowAnonymous]
    public async Task<IActionResult> GetLatestDpa()
    {
        var dpa = await _consentService.GetLatestDpaAsync();
        if (dpa == null) return NotFound("No DPA found");
        return Ok(dpa);
    }

    /// <summary>
    /// Check if current tenant has accepted the DPA
    /// </summary>
    [HttpGet("dpa/status")]
    public async Task<IActionResult> GetDpaStatus()
    {
        var tenantId = GetTenantId();
        var isAccepted = await _consentService.IsDpaAcceptedAsync(tenantId);
        return Ok(new { isAccepted });
    }

    /// <summary>
    /// Accept DPA terms for tenant
    /// </summary>
    [HttpPost("dpa/accept")]
    public async Task<IActionResult> AcceptDpa([FromBody] AcceptDpaRequest request)
    {
        var tenantId = GetTenantId();
        var userId = GetUserId();
        var success = await _consentService.AcceptDpaAsync(tenantId, userId, request.Version);

        if (!success) return BadRequest(new { error = "Failed to record DPA acceptance" });

        return Ok(new { success = true });
    }
}

public record ConsentRecordRequest(string ConsentType, bool Granted);
public record RevokeConsentRequest(string ConsentType);
public record AcceptDpaRequest(string Version);
public class HipaaBaaSignRequest
{
    public string AuthorizedSignatoryName { get; set; } = string.Empty;
    public string AuthorizedSignatoryTitle { get; set; } = string.Empty;
    public bool AgreesToTerms { get; set; }
    public string? BaaVersion { get; set; }
}
