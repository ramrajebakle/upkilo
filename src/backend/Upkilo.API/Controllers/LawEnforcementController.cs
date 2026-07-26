using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Controllers;

/// <summary>
/// Handles government and law enforcement data requests.
///
/// COMPANY: Upkilo Technologies Private Limited, incorporated in India.
///
/// APPLICABLE LEGAL FRAMEWORK
/// ───────────────────────────
/// Primary (Indian law):
///   • IT Act, 2000 — Section 69 (interception/decryption orders issued by Central/State Govt)
///   • IT Act, 2000 — Section 69A (content blocking orders)
///   • IT Act, 2000 — Section 69B (monitoring/collection of traffic data)
///   • DPDP Act, 2023 — Chapter VII (State/national security, public order, law enforcement exemptions)
///   • Code of Criminal Procedure, 1973 — Section 91 (police/court production orders)
///   • Indian court orders issued by competent civil and criminal courts
///
/// Extraterritorial (for EU/UK/US user data):
///   • GDPR Art. 48 — disclosures to third-country authorities require SCCs or adequacy basis
///   • UK GDPR — equivalent requirements for UK user data
///   • US legal process (MLAT treaties where applicable)
///
/// POLICY SUMMARY
/// ──────────────
/// We do not voluntarily share user data with any government, agency, or law enforcement.
/// Any disclosure requires:
///   (1) A valid legal instrument under the applicable jurisdiction's law.
///   (2) Formal written submission through this intake process with statutory citation.
///   (3) Legal-team review; overbroad/invalid requests are rejected or challenged.
///   (4) Disclosure of only the minimum data categories necessary to comply.
///   (5) User notification where legally permitted.
///
/// All requests — fulfilled, rejected, and challenged — are logged for transparency reporting.
/// </summary>
[ApiController]
[Route("api/v1/legal/government-requests")]
public class LawEnforcementController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<LawEnforcementController> _logger;
    private readonly IConfiguration _configuration;

    public LawEnforcementController(
        AppDbContext db,
        ILogger<LawEnforcementController> logger,
        IConfiguration configuration)
    {
        _db = db;
        _logger = logger;
        _configuration = configuration;
    }

    // ─── Public intake ────────────────────────────────────────────────────────

    /// <summary>
    /// POST /api/v1/legal/government-requests
    /// Submit a formal government / law enforcement data request for legal-team review.
    /// All requests are logged before any data is assessed, regardless of outcome.
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting("auth")] // 10 req/IP/min — prevents request flooding that drowns real legal requests
    public async Task<IActionResult> SubmitRequest(
        [FromBody] SubmitLegalRequestDto dto,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { error = "Invalid request payload.", details = ModelState });

        var referenceNumber = $"LDR-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";

        var record = new LegalDisclosureRequest
        {
            Id = Guid.NewGuid(),
            ReferenceNumber = referenceNumber,
            RequestType = dto.RequestType,
            IssuingAuthority = dto.IssuingAuthority,
            IssuingJurisdiction = dto.IssuingJurisdiction,
            StatutoryCitation = dto.StatutoryCitation,
            ReceivedAt = DateTime.UtcNow,
            ResponseDeadline = dto.ResponseDeadline,
            DataCategoriesRequested = dto.DataCategoriesRequested,
            Status = "Pending",
            UserNotified = false,
            NotificationLegallyProhibited = dto.NotificationProhibited,
        };

        _db.LegalDisclosureRequests.Add(record);
        await _db.SaveChangesAsync(ct);

        _logger.LogWarning(
            "[LEGAL-DISCLOSURE] New government request {ReferenceNumber} received. " +
            "Type={RequestType} Authority={Authority} Jurisdiction={Jurisdiction}",
            referenceNumber, dto.RequestType, dto.IssuingAuthority, dto.IssuingJurisdiction);

        var legalEmail = _configuration["Legal:ContactEmail"] ?? "legal@upkilo.com";

        return Accepted(new
        {
            referenceNumber,
            status = "Pending",
            message = $"Your request (ref: {referenceNumber}) has been logged and will be reviewed by our legal team. " +
                      "We will respond within the legally required timeframe or 14 business days, whichever is sooner.",
            legalContact = legalEmail,
            policy = "https://upkilo.com/legal/government-requests"
        });
    }

    // ─── Transparency report (public) ─────────────────────────────────────────

    /// <summary>
    /// GET /api/v1/legal/government-requests/transparency-report
    /// Returns aggregated annual statistics on government data requests.
    /// No individual request details are disclosed; only counts and categories.
    /// </summary>
    [HttpGet("transparency-report")]
    [AllowAnonymous]
    public async Task<IActionResult> GetTransparencyReport(
        [FromQuery] int year = 0,
        CancellationToken ct = default)
    {
        if (year == 0) year = DateTime.UtcNow.Year;

        var start = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddYears(1);

        var requests = await _db.LegalDisclosureRequests
            .Where(r => r.ReceivedAt >= start && r.ReceivedAt < end)
            .ToListAsync(ct);

        return Ok(new
        {
            reportYear = year,
            generatedAt = DateTime.UtcNow,
            summary = new
            {
                totalReceived = requests.Count,
                fulfilled = requests.Count(r => r.Status == "Fulfilled"),
                partiallyFulfilled = requests.Count(r => r.Status == "PartiallyFulfilled"),
                rejected = requests.Count(r => r.Status == "Rejected"),
                challenged = requests.Count(r => r.Status == "Challenged"),
                pending = requests.Count(r => r.Status == "Pending" || r.Status == "UnderReview"),
                usersNotified = requests.Count(r => r.UserNotified),
                notificationProhibited = requests.Count(r => r.NotificationLegallyProhibited)
            },
            byRequestType = requests
                .GroupBy(r => r.RequestType)
                .Select(g => new { type = g.Key, count = g.Count() })
                .OrderByDescending(x => x.count),
            byJurisdiction = requests
                .GroupBy(r => r.IssuingJurisdiction)
                .Select(g => new { jurisdiction = g.Key, count = g.Count() })
                .OrderByDescending(x => x.count),
            policy = new
            {
                company = "Upkilo Technologies Private Limited, incorporated in India",
                primaryLaw = "IT Act 2000, DPDP Act 2023, CrPC 1973 (India)",
                extraterritorialLaw = "GDPR (EU/EEA users), UK GDPR (UK users), applicable US process via MLAT",
                commitment = "Upkilo Technologies Private Limited does not voluntarily share user data with governments, " +
                             "agencies, or law enforcement. All disclosures require valid legal process under applicable law, " +
                             "are reviewed by legal counsel, are limited to the minimum data necessary, and are logged for accountability.",
                userNotificationDefault = "Where legally permitted under Indian law and applicable jurisdictional law, " +
                                          "Upkilo notifies affected users prior to or promptly after compliance.",
                challengePolicy = "Upkilo will challenge requests it believes to be overbroad, lacking proper legal basis, " +
                                  "or inconsistent with the DPDP Act, IT Act, GDPR, or other applicable privacy laws.",
                contactEmail = _configuration["Legal:ContactEmail"] ?? "legal@upkilo.com"
            }
        });
    }

    // ─── Internal admin management (SuperAdmin only) ──────────────────────────

    /// <summary>
    /// GET /api/v1/legal/government-requests
    /// List all government requests. SuperAdmin only. Internal use.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "SuperAdmin")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> ListRequests(
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        var query = _db.LegalDisclosureRequests.AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(r => r.Status == status);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(r => r.ReceivedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return Ok(new { total, page, pageSize, items });
    }

    /// <summary>
    /// PATCH /api/v1/legal/government-requests/{id}/status
    /// Update the status of a pending request after legal review. SuperAdmin only.
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "SuperAdmin")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        [FromBody] UpdateRequestStatusDto dto,
        CancellationToken ct)
    {
        var record = await _db.LegalDisclosureRequests.FindAsync(new object[] { id }, ct);
        if (record == null) return NotFound();

        var validStatuses = new[]
        {
            "Pending", "UnderReview", "Fulfilled", "PartiallyFulfilled",
            "Rejected", "Challenged", "Withdrawn"
        };
        if (!validStatuses.Contains(dto.Status))
            return BadRequest(new { error = "Invalid status value.", valid = validStatuses });

        var reviewerIdClaim = (User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
        record.Status = dto.Status;
        record.LegalCounselNotes = dto.Notes;
        record.DataCategoriesProvided = dto.DataCategoriesProvided;
        record.RejectionReason = dto.RejectionReason;
        record.ReviewedByUserId = reviewerIdClaim != null ? Guid.Parse(reviewerIdClaim) : null;
        record.ReviewedAt = DateTime.UtcNow;

        if (dto.Status is "Fulfilled" or "PartiallyFulfilled")
            record.FulfilledAt = DateTime.UtcNow;

        if (dto.UserNotified)
        {
            record.UserNotified = true;
            record.UserNotifiedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);

        _logger.LogWarning(
            "[LEGAL-DISCLOSURE] Request {ReferenceNumber} status updated to {Status} by {ReviewerId}",
            record.ReferenceNumber, dto.Status, reviewerIdClaim ?? "unknown");

        return Ok(new { referenceNumber = record.ReferenceNumber, status = record.Status });
    }
}

// ─── DTOs ─────────────────────────────────────────────────────────────────────

/// <summary>
/// Valid RequestType values — includes Indian-law-specific instruments.
/// IT Act S.69 Order | IT Act S.69A Blocking Order | IT Act S.69B Monitoring Order |
/// CrPC S.91 Production Order | Court Order (Civil) | Court Order (Criminal) |
/// Regulatory Order (SEBI/RBI/ED/CBI) | MLAT Request | Subpoena | SearchWarrant |
/// NationalSecurityLetter | AdministrativeRequest | InformalRequest | Other
/// </summary>
public record SubmitLegalRequestDto(
    [Required, MaxLength(100)] string RequestType,
    [Required, MaxLength(200)] string IssuingAuthority,
    [Required, MaxLength(100)] string IssuingJurisdiction,
    [MaxLength(500)] string? StatutoryCitation,
    DateTime? ResponseDeadline,
    [MaxLength(1000)] string? DataCategoriesRequested,
    bool NotificationProhibited
);

public record UpdateRequestStatusDto(
    string Status,
    string? Notes,
    string? DataCategoriesProvided,
    string? RejectionReason,
    bool UserNotified
);
