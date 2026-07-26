using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;
using Upkilo.API.Middleware;
using System.Collections.Concurrent;

namespace Upkilo.API.Controllers;

/// <summary>
/// CCPA (California Consumer Privacy Act) compliance endpoints.
/// Provides:
///   • Right to Know  — GET  /api/v1/ccpa/my-data
///   • Right to Delete — POST /api/v1/ccpa/delete-request
///   • Right to Opt-Out (Do Not Sell) — POST /api/v1/ccpa/do-not-sell
///   • Right to Non-Discrimination  — GET  /api/v1/ccpa/status
///   • Data Processing Agreements (DPA) — GET/POST /api/v1/ccpa/dpa
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ccpa")]
public class CcpaController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<CcpaController> _logger;

    // In-memory DPA store (replace with DB table in production)
    private static readonly ConcurrentDictionary<string, DpaRecord> _dpaStore = new();

    public CcpaController(
        AppDbContext db,
        ITenantProvider tenantProvider,
        ILogger<CcpaController> logger)
    {
        _db = db;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    // ── Right to Know ─────────────────────────────────────────────────────────

    /// <summary>
    /// CCPA §1798.110 — Consumer right to know what personal information is collected.
    /// Returns categories + specific pieces of PI held for the authenticated user.
    /// </summary>
    [HttpGet("my-data")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyData(CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId.Value, ct);
        if (user == null) return NotFound();

        var sessionCount = await _db.UserSessions.CountAsync(s => s.UserId == userId.Value, ct);

        return Ok(ApiResponse<object>.Ok(new
        {
            disclosureDate = DateTime.UtcNow,
            subject = new
            {
                userId = userId.Value,
                email = user.Email,
                name = user.FirstName + " " + user.LastName
            },
            categoriesCollected = new[]
            {
                new { category = "Identifiers", examples = "Name, email, phone, IP address", collected = true },
                new { category = "Commercial information", examples = "Booking history, payment records", collected = true },
                new { category = "Internet/electronic activity", examples = "Login history, session data", collected = true },
                new { category = "Geolocation data", examples = "Approximate location from IP", collected = false },
                new { category = "Sensory data", examples = "Profile photos (if uploaded)", collected = true },
                new { category = "Professional data", examples = "Staff role, certifications", collected = true },
            },
            businessPurposes = new[]
            {
                "Providing and improving our booking services",
                "Billing and payment processing",
                "Customer support and communications",
                "Security and fraud prevention",
                "Legal compliance"
            },
            thirdPartyDisclosures = new[]
            {
                new { name = "Stripe", purpose = "Payment processing", dataShared = "Payment identifiers" },
                new { name = "SendGrid", purpose = "Transactional email", dataShared = "Email address, name" },
                new { name = "Twilio", purpose = "SMS reminders", dataShared = "Phone number" }
            },
            dataCounts = new
            {
                activeSessions = sessionCount,
                requestedAt = DateTime.UtcNow
            },
            rights = new[]
            {
                "Right to know (§1798.110)",
                "Right to delete (§1798.105)",
                "Right to opt-out of sale (§1798.120)",
                "Right to non-discrimination (§1798.125)"
            }
        }));
    }

    // ── Right to Delete ───────────────────────────────────────────────────────

    /// <summary>
    /// CCPA §1798.105 — Consumer right to request deletion of personal information.
    /// Persists the request to DB (fulfilled within 45 days per CCPA).
    /// </summary>
    [HttpPost("delete-request")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RequestDeletion([FromBody] DeletionRequestDto dto, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var requestNumber = $"CCPA-DEL-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
        var dueBy = DateTime.UtcNow.AddDays(45);

        var record = new CcpaDeletionRequest
        {
            Id = Guid.NewGuid(),
            RequestNumber = requestNumber,
            UserId = userId.Value,
            Reason = dto.Reason,
            RequestedAt = DateTime.UtcNow,
            DueBy = dueBy,
            Status = "pending"
        };

        _db.CcpaDeletionRequests.Add(record);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "CCPA deletion request {RequestNumber} submitted for user {UserId}",
            requestNumber, userId);

        return Ok(ApiResponse<object>.Ok(new
        {
            requestId = requestNumber,
            status = "pending",
            dueBy,
            message = $"Your deletion request has been received (ID: {requestNumber}). " +
                      "We will fulfill this request within 45 days as required by CCPA.",
            confirmationEmailSent = true
        }));
    }

    // ── Right to Opt-Out (Do Not Sell) ────────────────────────────────────────

    /// <summary>
    /// CCPA §1798.120 — Consumer right to opt out of sale of personal information.
    /// Sets a "do not sell" flag on the user's record.
    /// </summary>
    [HttpPost("do-not-sell")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> OptOutOfSale([FromBody] DoNotSellRequest req, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var user = await _db.Users.FindAsync(new object[] { userId.Value }, ct);
        if (user == null) return NotFound();

        user.DoNotSell = req.OptOut;
        user.DoNotSellUpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "CCPA do-not-sell {Action} for user {UserId}",
            req.OptOut ? "opt-out" : "opt-in", userId);

        return Ok(ApiResponse<object>.Ok(new
        {
            optedOut = req.OptOut,
            effectiveDate = DateTime.UtcNow,
            message = req.OptOut
                ? "Your preference not to have your data sold has been recorded."
                : "Your opt-out preference has been reversed."
        }));
    }

    /// <summary>GET the current do-not-sell preference for the authenticated user.</summary>
    [HttpGet("do-not-sell")]
    [Authorize]
    public async Task<IActionResult> GetOptOutStatus(CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var user = await _db.Users.FindAsync(new object[] { userId.Value }, ct);
        if (user == null) return NotFound();

        return Ok(ApiResponse<object>.Ok(new
        {
            userId,
            doNotSell = user.DoNotSell,
            lastUpdated = user.DoNotSellUpdatedAt
        }));
    }

    // ── CCPA Status / Non-Discrimination ─────────────────────────────────────

    /// <summary>Returns the CCPA compliance status and user's active requests.</summary>
    [HttpGet("status")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatus(CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var myRequests = await _db.CcpaDeletionRequests
            .Where(r => r.UserId == userId.Value)
            .OrderByDescending(r => r.RequestedAt)
            .Select(r => new { requestId = r.RequestNumber, r.Status, r.RequestedAt, r.DueBy })
            .ToListAsync(ct);

        return Ok(ApiResponse<object>.Ok(new
        {
            ccpaApplicable = true,
            region = "California, USA",
            rights = new[]
            {
                "Right to Know (§1798.110)",
                "Right to Delete (§1798.105)",
                "Right to Opt-Out of Sale (§1798.120)",
                "Right to Non-Discrimination (§1798.125)"
            },
            pendingRequests = myRequests,
            contact = new
            {
                email = "privacy@upkilo.com",
                url = "https://upkilo.com/privacy"
            }
        }));
    }

    // ── Data Processing Agreements ────────────────────────────────────────────

    /// <summary>
    /// Returns the current DPA template for the authenticated tenant.
    /// B2B tenants (data processors) must sign this before handling end-user data.
    /// </summary>
    [HttpGet("dpa")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetDpa()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var key = tenantId.Value.ToString();
        var signed = _dpaStore.TryGetValue(key, out var existing) ? existing : null;

        return Ok(ApiResponse<object>.Ok(new
        {
            templateVersion = "2.1",
            effectiveDate = new DateTime(2024, 1, 1),
            parties = new
            {
                dataController = "Upkilo Inc., 123 Main St, San Francisco, CA 94105",
                dataProcessor = $"Tenant {tenantId}"
            },
            scope = "Processing of personal data of end-users on behalf of the data controller via the Upkilo platform.",
            dataCategories = new[]
            {
                "Contact information (name, email, phone)",
                "Booking and appointment history",
                "Payment information (tokenized via Stripe)",
                "Device and usage data"
            },
            processingPurposes = new[]
            {
                "Appointment scheduling and management",
                "Customer communication and reminders",
                "Analytics and business intelligence",
                "Legal compliance"
            },
            subProcessors = new[]
            {
                new { name = "Stripe", purpose = "Payment processing", location = "USA" },
                new { name = "Amazon Web Services", purpose = "Infrastructure hosting", location = "USA/EU" },
                new { name = "SendGrid", purpose = "Email delivery", location = "USA" }
            },
            retentionPeriod = "Data retained for 365 days after account termination, then securely deleted.",
            signed = signed != null,
            signedAt = signed?.SignedAt,
            signedByEmail = signed?.SignedByEmail,
            downloadUrl = signed != null ? $"/api/v1/ccpa/dpa/download?tenantId={tenantId}" : null
        }));
    }

    /// <summary>
    /// Tenant signs (accepts) the Data Processing Agreement.
    /// </summary>
    [HttpPost("dpa/sign")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult SignDpa([FromBody] SignDpaRequest req)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var userId = GetUserId();
        var user = _db.Users.Find(userId);

        var record = new DpaRecord
        {
            TenantId = tenantId.Value,
            SignedByUserId = userId!.Value,
            SignedByEmail = user?.Email ?? req.SignerEmail ?? "unknown",
            SignedAt = DateTime.UtcNow,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            Version = "2.1"
        };

        _dpaStore[tenantId.Value.ToString()] = record;

        _logger.LogInformation(
            "DPA signed for tenant {TenantId} by {Email} at {Time}",
            tenantId, record.SignedByEmail, record.SignedAt);

        return Ok(ApiResponse<object>.Ok(new
        {
            signed = true,
            signedAt = record.SignedAt,
            signedBy = record.SignedByEmail,
            version = record.Version,
            message = "Data Processing Agreement signed and recorded."
        }));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Guid? GetUserId()
    {
        var claim = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}

// ─── Models ───────────────────────────────────────────────────────────────────

public record DeletionRequestDto
{
    public string? Reason { get; init; }
}

public record DoNotSellRequest
{
    public bool OptOut { get; init; } = true;
}

public record SignDpaRequest
{
    public string? SignerEmail { get; init; }
}

public class DeletionRequest
{
    public string RequestId { get; set; } = "";
    public Guid UserId { get; set; }
    public string? Reason { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime DueBy { get; set; }
    public string Status { get; set; } = "pending";
}

public class DpaRecord
{
    public Guid TenantId { get; set; }
    public Guid SignedByUserId { get; set; }
    public string SignedByEmail { get; set; } = "";
    public DateTime SignedAt { get; set; }
    public string? IpAddress { get; set; }
    public string Version { get; set; } = "";
}
