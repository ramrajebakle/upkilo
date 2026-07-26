using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Distributed;
using Upkilo.API.Middleware;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;
using Upkilo.Infrastructure.Services;

namespace Upkilo.API.Controllers;

/// <summary>
/// Migration wizard — import clients from Mindbody, Vagaro, Acuity CSV exports.
/// Endpoint flow: upload → validate → preview → execute.
/// Sessions are stored in Redis with a 2-hour TTL — consistent across instances and
/// survive restarts.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/migration")]
[Authorize]
public class MigrationWizardController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly IEmailService _emailService;
    private readonly ILogger<MigrationWizardController> _logger;
    private readonly IDistributedCache _cache;

    private static readonly TimeSpan SessionTtl = TimeSpan.FromHours(2);

    public MigrationWizardController(
        AppDbContext context,
        ITenantProvider tenantProvider,
        IEmailService emailService,
        ILogger<MigrationWizardController> logger,
        IDistributedCache cache)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _emailService = emailService;
        _logger = logger;
        _cache = cache;
    }

    private string SessionKey(Guid sessionId) => $"migration:session:{sessionId}";

    private async Task<MigrationSession?> LoadSessionAsync(Guid sessionId)
    {
        var json = await _cache.GetStringAsync(SessionKey(sessionId));
        return json == null ? null : JsonSerializer.Deserialize<MigrationSession>(json);
    }

    private Task SaveSessionAsync(MigrationSession session)
        => _cache.SetStringAsync(SessionKey(session.SessionId), JsonSerializer.Serialize(session),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = SessionTtl });

    private Task DeleteSessionAsync(Guid sessionId)
        => _cache.RemoveAsync(SessionKey(sessionId));

    /// <summary>POST /api/v1/migration/upload — upload CSV and detect source platform.</summary>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload(IFormFile file, [FromQuery] string? platform = null)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        if (file == null || file.Length == 0)
            return BadRequest(ApiResponse.Fail("No file provided"));

        if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            return BadRequest(ApiResponse.Fail("Only CSV files are supported"));

        if (file.Length > 10 * 1024 * 1024)
            return BadRequest(ApiResponse.Fail("File too large (max 10MB)"));

        using var reader = new StreamReader(file.OpenReadStream());
        var content = await reader.ReadToEndAsync();
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length < 2)
            return BadRequest(ApiResponse.Fail("CSV must have a header row and at least one data row"));

        var headers = lines[0].Split(',').Select(h => h.Trim('"', ' ').ToLowerInvariant()).ToArray();

        // Auto-detect platform from headers
        var detectedPlatform = platform ?? DetectPlatform(headers);
        var parser = GetParser(detectedPlatform);

        if (parser == null)
            return BadRequest(ApiResponse.Fail($"Unsupported platform: {detectedPlatform}. Supported: mindbody, vagaro, acuity, generic"));

        var sessionId = Guid.NewGuid();
        var parsed = parser.Parse(lines);

        await SaveSessionAsync(new MigrationSession
        {
            SessionId = sessionId,
            TenantId = tenantId.Value,
            Platform = detectedPlatform,
            Headers = headers,
            ParsedClients = parsed,
            CreatedAt = DateTime.UtcNow
        });

        return Ok(ApiResponse<object>.Ok(new
        {
            sessionId,
            platform = detectedPlatform,
            totalRows = parsed.Count,
            headers,
            sample = parsed.Take(3)
        }));
    }

    /// <summary>GET /api/v1/migration/{sessionId}/preview — preview with dedup analysis.</summary>
    [HttpGet("{sessionId}/preview")]
    public async Task<IActionResult> Preview(Guid sessionId)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var session = await LoadSessionAsync(sessionId);
        if (session == null || session.TenantId != tenantId.Value)
            return NotFound(ApiResponse.Fail("Session not found"));

        // Dedup: find clients already in DB by email or phone
        var existingEmails = _context.Clients
            .Where(c => c.TenantId == tenantId.Value)
            .Select(c => c.Email)
            .Where(e => e != null)
            .ToHashSet()!;

        var existingPhones = _context.Clients
            .Where(c => c.TenantId == tenantId.Value)
            .Select(c => c.Phone)
            .Where(p => p != null)
            .ToHashSet()!;

        var newClients = session.ParsedClients
            .Where(c => !existingEmails.Contains(c.Email) && !existingPhones.Contains(c.Phone))
            .ToList();

        var duplicates = session.ParsedClients.Count - newClients.Count;

        return Ok(ApiResponse<object>.Ok(new
        {
            sessionId,
            platform = session.Platform,
            totalParsed = session.ParsedClients.Count,
            toImport = newClients.Count,
            duplicatesSkipped = duplicates,
            sample = newClients.Take(5)
        }));
    }

    /// <summary>POST /api/v1/migration/{sessionId}/execute — import clients, send completion email.</summary>
    [HttpPost("{sessionId}/execute")]
    public async Task<IActionResult> Execute(Guid sessionId)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var session = await LoadSessionAsync(sessionId);
        if (session == null || session.TenantId != tenantId.Value)
            return NotFound(ApiResponse.Fail("Session not found"));

        var existingEmails = _context.Clients
            .Where(c => c.TenantId == tenantId.Value && c.Email != null)
            .Select(c => c.Email!)
            .ToHashSet();

        var existingPhones = _context.Clients
            .Where(c => c.TenantId == tenantId.Value && c.Phone != null)
            .Select(c => c.Phone!)
            .ToHashSet();

        var toImport = session.ParsedClients
            .Where(c => (c.Email == null || !existingEmails.Contains(c.Email)) &&
                        (c.Phone == null || !existingPhones.Contains(c.Phone)))
            .Select(c => new Client
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId.Value,
                FirstName = c.FirstName,
                LastName = c.LastName,
                Email = c.Email,
                Phone = c.Phone,
                DateOfBirth = c.DateOfBirth.HasValue ? DateOnly.FromDateTime(c.DateOfBirth.Value) : null,
                Notes = $"Imported from {session.Platform} on {DateTime.UtcNow:yyyy-MM-dd}",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                MarketingConsent = c.MarketingConsent
            })
            .ToList();

        _context.Clients.AddRange(toImport);
        await _context.SaveChangesAsync();

        await DeleteSessionAsync(sessionId);

        var tenant = await _context.Tenants.FindAsync(tenantId.Value);
        if (tenant?.Email != null)
        {
            await _emailService.SendSystemEmailAsync(
                tenant.Email,
                $"Your {session.Platform} data is ready!",
                $"<h2>Migration Complete!</h2>" +
                $"<p>We imported <strong>{toImport.Count} clients</strong> from your {session.Platform} export.</p>" +
                $"<p>View your clients at <a href='/dashboard/clients'>Dashboard → Clients</a></p>");
        }

        _logger.LogInformation("[Migration] Imported {Count} clients from {Platform} for tenant {TenantId}", toImport.Count, session.Platform, tenantId.Value);

        return Ok(ApiResponse<object>.Ok(new
        {
            imported = toImport.Count,
            platform = session.Platform,
            message = $"Successfully imported {toImport.Count} clients from {session.Platform}"
        }));
    }

    private static string DetectPlatform(string[] headers)
    {
        if (headers.Any(h => h.Contains("mindbody") || h.Contains("client_index"))) return "mindbody";
        if (headers.Any(h => h.Contains("vagaro") || h.Contains("booking_notes"))) return "vagaro";
        if (headers.Any(h => h.Contains("acuity") || h.Contains("appointment_type"))) return "acuity";
        return "generic";
    }

    private static ICsvClientParser? GetParser(string platform) => platform switch
    {
        "mindbody" => new MindbodyCsvParser(),
        "vagaro" => new VagaroCsvParser(),
        "acuity" => new AcuityCsvParser(),
        "generic" => new GenericCsvParser(),
        _ => null
    };
}

// ──────────────────────────────────────────────────────────────────────────
// Parsers
// ──────────────────────────────────────────────────────────────────────────

public interface ICsvClientParser
{
    List<ImportedClient> Parse(string[] lines);
}

public class ImportedClient
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public bool MarketingConsent { get; set; } = true;
}

public class MindbodyCsvParser : ICsvClientParser
{
    public List<ImportedClient> Parse(string[] lines)
    {
        if (lines.Length < 2) return new();
        var headers = ParseCsvLine(lines[0]);
        var idx = BuildIndex(headers, new[] { "first_name", "firstname", "first" },
                                       new[] { "last_name", "lastname", "last" },
                                       new[] { "email", "email_address" },
                                       new[] { "phone", "mobile", "cell_phone", "home_phone" },
                                       new[] { "birth_date", "dob", "date_of_birth" });

        return lines.Skip(1).Select(line => ParseLine(line, idx)).Where(c => c != null).Cast<ImportedClient>().ToList();
    }

    private static ImportedClient? ParseLine(string line, (int fn, int ln, int em, int ph, int dob) idx)
    {
        var fields = ParseCsvLine(line);
        if (fields.Length == 0) return null;
        return new ImportedClient
        {
            FirstName = idx.fn >= 0 && idx.fn < fields.Length ? fields[idx.fn].Trim() : "",
            LastName = idx.ln >= 0 && idx.ln < fields.Length ? fields[idx.ln].Trim() : "",
            Email = idx.em >= 0 && idx.em < fields.Length ? fields[idx.em].Trim().NullIfEmpty() : null,
            Phone = idx.ph >= 0 && idx.ph < fields.Length ? fields[idx.ph].Trim().NullIfEmpty() : null,
            DateOfBirth = idx.dob >= 0 && idx.dob < fields.Length
                ? DateTime.TryParse(fields[idx.dob], out var d) ? d : null
                : null
        };
    }

    private static (int fn, int ln, int em, int ph, int dob) BuildIndex(
        string[] headers, string[] fnAliases, string[] lnAliases, string[] emAliases, string[] phAliases, string[] dobAliases)
    {
        int Find(string[] aliases) =>
            Array.FindIndex(headers, h => aliases.Contains(h.ToLowerInvariant().Replace(" ", "_")));

        return (Find(fnAliases), Find(lnAliases), Find(emAliases), Find(phAliases), Find(dobAliases));
    }

    protected static string[] ParseCsvLine(string line)
    {
        var result = new List<string>();
        bool inQuotes = false;
        var current = new System.Text.StringBuilder();
        foreach (var c in line)
        {
            if (c == '"') { inQuotes = !inQuotes; }
            else if (c == ',' && !inQuotes) { result.Add(current.ToString()); current.Clear(); }
            else { current.Append(c); }
        }
        result.Add(current.ToString());
        return result.ToArray();
    }
}

public class VagaroCsvParser : MindbodyCsvParser
{
    public new List<ImportedClient> Parse(string[] lines) => base.Parse(lines);
}

public class AcuityCsvParser : MindbodyCsvParser
{
    public new List<ImportedClient> Parse(string[] lines) => base.Parse(lines);
}

public class GenericCsvParser : MindbodyCsvParser
{
    public new List<ImportedClient> Parse(string[] lines) => base.Parse(lines);
}

public class MigrationSession
{
    public Guid SessionId { get; set; }
    public Guid TenantId { get; set; }
    public string Platform { get; set; } = string.Empty;
    public string[] Headers { get; set; } = Array.Empty<string>();
    public List<ImportedClient> ParsedClients { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

internal static class StringExtensions
{
    public static string? NullIfEmpty(this string s) => string.IsNullOrWhiteSpace(s) ? null : s;
}
