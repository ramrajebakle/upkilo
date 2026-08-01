using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Text.RegularExpressions;
using Upkilo.API.Attributes;
using Upkilo.API.Middleware;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Upkilo.API.Controllers;

/// <summary>
/// Bulk SMS opt-in import — validates, deduplicates, and mass-enrolls phone numbers
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/sms/opt-in")]
[Authorize]
[FeatureGuard("sms_reminders")]
public class BulkSmsController : ControllerBase
{
    private readonly ITenantProvider _tenantProvider;
    private readonly AppDbContext _context;
    private readonly ILogger<BulkSmsController> _logger;
    private readonly ISmsService _smsService;

    // Regex: E.164 or common US/international formats
    private static readonly Regex PhoneRegex = new(
        @"^\+?[\d\s\-().]{7,15}$",
        RegexOptions.Compiled);

    public BulkSmsController(
        ITenantProvider tenantProvider,
        AppDbContext context,
        ILogger<BulkSmsController> logger,
        ISmsService smsService)
    {
        _tenantProvider = tenantProvider;
        _context = context;
        _logger = logger;
        _smsService = smsService;
    }

    private Guid GetTenantId() => _tenantProvider.GetTenantId()
        ?? throw new UnauthorizedAccessException("Tenant not found");

    /// <summary>
    /// POST /api/v1/sms/opt-in/bulk-import
    /// Parse CSV, validate phone numbers, create/update Client records,
    /// and optionally send opt-in confirmation SMS
    /// </summary>
    [HttpPost("bulk-import")]
    public async Task<IActionResult> BulkImport([FromBody] BulkSmsImportRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CsvContent))
            return BadRequest(ApiResponse.Fail("CSV content is required"));

        var tenantId = GetTenantId();
        var lines = request.CsvContent.Trim().Split('\n');

        if (lines.Length < 2)
            return BadRequest(ApiResponse.Fail("CSV must have header + at least one data row"));

        // Parse header
        var headers = lines[0].Split(',').Select(h => h.Trim().Trim('"').ToLowerInvariant()).ToArray();
        var phoneIdx = Array.FindIndex(headers, h => h.Contains("phone") || h.Contains("mobile"));
        var firstIdx = Array.FindIndex(headers, h => h.Contains("first"));
        var lastIdx = Array.FindIndex(headers, h => h.Contains("last"));
        var emailIdx = Array.FindIndex(headers, h => h.Contains("email"));
        var tagsIdx = Array.FindIndex(headers, h => h.Contains("tag"));

        if (phoneIdx < 0)
            return BadRequest(ApiResponse.Fail("CSV must have a 'phone' column"));

        var results = new List<ImportContactResult>();
        var imported = 0;
        var duplicates = 0;
        var invalid = 0;

        // Load existing phone numbers for duplicate detection
        var existingPhones = _context.Clients
            .Where(c => c.TenantId == tenantId && !c.IsDeleted)
            .Select(c => c.Phone ?? "")
            .Where(p => p != "")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        for (var i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;

            var cols = line.Split(',').Select(c => c.Trim().Trim('"')).ToArray();
            var phone = phoneIdx < cols.Length ? cols[phoneIdx] : "";
            var firstName = firstIdx >= 0 && firstIdx < cols.Length ? cols[firstIdx] : null;
            var lastName = lastIdx >= 0 && lastIdx < cols.Length ? cols[lastIdx] : null;
            var email = emailIdx >= 0 && emailIdx < cols.Length ? cols[emailIdx] : null;
            var tags = tagsIdx >= 0 && tagsIdx < cols.Length ? cols[tagsIdx] : null;

            var result = new ImportContactResult { Row = i + 1, Phone = phone };

            if (string.IsNullOrWhiteSpace(phone))
            {
                result.Status = "invalid";
                result.Error = "Missing phone number";
                invalid++;
            }
            else if (!PhoneRegex.IsMatch(phone))
            {
                result.Status = "invalid";
                result.Error = "Invalid phone number format";
                invalid++;
            }
            else if (existingPhones.Contains(phone))
            {
                result.Status = "duplicate";
                duplicates++;
            }
            else
            {
                // Create new Client record with SMS opt-in
                var normalizedPhone = NormalizePhone(phone);
                var client = new Upkilo.Core.Entities.Client
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    FirstName = firstName ?? "Unknown",
                    LastName = lastName ?? "",
                    Email = string.IsNullOrWhiteSpace(email) ? null : email,
                    Phone = normalizedPhone,
                    SmsConsent = true,
                    MarketingConsent = true,
                    Source = "bulk_import",
                    Tags = new List<string>(CombineTags(tags, request.DefaultTags)),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsActive = true,
                };

                _context.Clients.Add(client);
                existingPhones.Add(normalizedPhone);
                result.Status = "valid";
                result.ClientId = client.Id.ToString();
                imported++;
            }

            result.FirstName = firstName;
            result.LastName = lastName;
            result.Email = email;
            results.Add(result);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Bulk SMS import for tenant {TenantId}: {Imported} imported, {Dups} duplicates, {Invalid} invalid",
            tenantId, imported, duplicates, invalid);

        // Trigger opt-in SMS via ISmsService for imported contacts
        if (request.SendOptInMessage && imported > 0)
        {
            var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId);
            var businessName = tenant?.Name ?? "our business";

            foreach (var result in results)
            {
                if (result.Status == "valid" && !string.IsNullOrEmpty(result.Phone))
                {
                    var msg = request.OptInMessageTemplate
                        .Replace("{{firstName}}", result.FirstName ?? "there")
                        .Replace("{{businessName}}", businessName);

                    await _smsService.SendSmsAsync(tenantId, result.Phone, msg,
                        result.ClientId != null ? Guid.Parse(result.ClientId) : null);
                }
            }
        }

        return Ok(ApiResponse<object>.Ok(new
        {
            total = results.Count,
            imported,
            duplicates,
            invalid,
            contacts = results,
            optInSmsSent = request.SendOptInMessage ? imported : 0,
        }));
    }

    private static string NormalizePhone(string phone)
    {
        var digits = Regex.Replace(phone, @"[^\d+]", "");
        if (!digits.StartsWith("+") && digits.Length == 10)
            digits = "+1" + digits; // Default to US
        return digits;
    }

    private static string[] CombineTags(string? rowTags, string[]? defaultTags)
    {
        var tags = new List<string>();
        if (!string.IsNullOrWhiteSpace(rowTags))
            tags.AddRange(rowTags.Split(',').Select(t => t.Trim()).Where(t => t.Length > 0));
        if (defaultTags != null)
            tags.AddRange(defaultTags);
        return tags.Distinct().ToArray();
    }
}

public class BulkSmsImportRequest
{
    public string CsvContent { get; set; } = string.Empty;
    public bool SendOptInMessage { get; set; } = true;
    public string OptInMessageTemplate { get; set; } = "Hi {{firstName}}! You've been added to receive updates from {{businessName}}. Reply STOP to unsubscribe.";
    public string[]? DefaultTags { get; set; }
}

public class ImportContactResult
{
    public int Row { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string Status { get; set; } = "valid"; // valid, invalid, duplicate
    public string? Error { get; set; }
    public string? ClientId { get; set; }
}
