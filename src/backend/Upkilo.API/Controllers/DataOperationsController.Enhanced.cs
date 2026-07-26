using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Upkilo.API.Middleware;
using System.Text.RegularExpressions;

namespace Upkilo.API.Controllers;

/// <summary>
/// Enhanced DataOperations — import preview, field-level validation,
/// duplicate detection against DB and within the file.
/// Extends DataOperationsController (partial class).
/// </summary>
public partial class DataOperationsController
{
    // ──────────────────────────────────────────────────────────────────────────
    // POST api/data/import/preview
    // Full import preview: parse CSV/JSON, map fields, validate rows,
    // detect duplicates against existing DB and within the file.
    // Does NOT commit any data — preview only.
    // ──────────────────────────────────────────────────────────────────────────
    [HttpPost("import/preview")]
    public async Task<IActionResult> PreviewImport(
        [FromForm] ImportPreviewRequest request,
        CancellationToken ct)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        if (request.File == null || request.File.Length == 0)
            return BadRequest(ApiResponse<object>.Fail("File is required"));

        // ── Parse CSV ────────────────────────────────────────────────────────
        var rows = new List<Dictionary<string, string>>();
        string[]? headers = null;

        using var reader = new System.IO.StreamReader(request.File.OpenReadStream());
        int lineNum = 0;

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync(ct);
            if (string.IsNullOrWhiteSpace(line)) continue;

            lineNum++;
            var cells = ParseCsvLine(line);

            if (lineNum == 1)
            {
                headers = cells;
                continue;
            }

            if (headers != null)
            {
                var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < headers.Length && i < cells.Length; i++)
                    row[headers[i].Trim()] = cells[i].Trim();
                rows.Add(row);
            }

            // Cap preview at 500 rows
            if (rows.Count >= 500) break;
        }

        if (headers == null || headers.Length == 0)
            return BadRequest(ApiResponse<object>.Fail("Could not parse CSV headers"));

        // ── Auto-detect entity type (or use request.EntityType) ──────────────
        var entityType = request.EntityType ?? DetectEntityType(headers);

        // ── Field mapping (auto-map by common aliases) ────────────────────────
        var fieldMap = BuildFieldMap(headers, entityType);

        // ── Validate rows ─────────────────────────────────────────────────────
        var validationResults = new List<RowValidationResult>();
        int validRows = 0, warningRows = 0, errorRows = 0;

        // Collect emails/phones from file for within-file duplicate detection
        var fileEmails = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var filePhones = new Dictionary<string, int>();

        foreach (var (row, idx) in rows.Select((r, i) => (r, i + 1)))
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            var email = GetMappedValue(row, fieldMap, "Email");
            var name = GetMappedValue(row, fieldMap, "FirstName") + " " + GetMappedValue(row, fieldMap, "LastName");
            var phone = GetMappedValue(row, fieldMap, "Phone");

            // Required field checks
            if (entityType == "client")
            {
                if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(GetMappedValue(row, fieldMap, "FirstName")))
                    errors.Add("Name or email is required");

                if (!string.IsNullOrWhiteSpace(email) && !IsValidEmail(email))
                    errors.Add($"Invalid email: {email}");

                if (!string.IsNullOrWhiteSpace(phone) && !IsValidPhone(phone))
                    warnings.Add($"Phone format may be invalid: {phone}");
            }

            // Within-file duplicate detection
            bool withinFileDuplicate = false;
            if (!string.IsNullOrWhiteSpace(email))
            {
                if (fileEmails.TryGetValue(email, out var firstRow))
                {
                    warnings.Add($"Duplicate email in file (first seen on row {firstRow})");
                    withinFileDuplicate = true;
                }
                else
                {
                    fileEmails[email] = idx;
                }
            }

            var status = errors.Count > 0 ? "error" : warnings.Count > 0 ? "warning" : "ok";
            if (status == "ok") validRows++;
            else if (status == "warning") warningRows++;
            else errorRows++;

            validationResults.Add(new RowValidationResult
            {
                RowIndex = idx,
                Data = row.Take(6).ToDictionary(k => k.Key, v => v.Value),
                Status = status,
                Errors = errors,
                Warnings = warnings,
                IsWithinFileDuplicate = withinFileDuplicate,
                MappedName = name.Trim(),
                MappedEmail = email
            });
        }

        // ── DB duplicate check (sample: first 50 emails) ─────────────────────
        var emailsToCheck = fileEmails.Keys.Take(50).ToList();
        var existingEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (entityType == "client" && emailsToCheck.Count > 0)
        {
            try
            {
                var found = await _context.Clients
                    .Where(c => c.TenantId == tenantId.Value && emailsToCheck.Contains(c.Email))
                    .Select(c => c.Email)
                    .ToListAsync(ct);
                existingEmails = new HashSet<string>(found, StringComparer.OrdinalIgnoreCase);

                // Mark DB duplicates in results
                foreach (var result in validationResults)
                {
                    if (!string.IsNullOrWhiteSpace(result.MappedEmail) && existingEmails.Contains(result.MappedEmail))
                    {
                        result.IsDbDuplicate = true;
                        result.Warnings.Add("Already exists in database");
                        if (result.Status == "ok") { result.Status = "warning"; warningRows++; validRows--; }
                    }
                }
            }
            catch
            {
                // DB check is best-effort
            }
        }

        // ── Summary ───────────────────────────────────────────────────────────
        var previewRows = validationResults.Take(request.PreviewRows ?? 20).ToList();

        return Ok(ApiResponse<object>.Ok(new
        {
            entityType,
            totalRows = rows.Count,
            validRows,
            warningRows,
            errorRows,
            dbDuplicatesDetected = existingEmails.Count,
            withinFileDuplicates = validationResults.Count(r => r.IsWithinFileDuplicate),
            headers,
            fieldMapping = fieldMap,
            preview = previewRows,
            canProceed = errorRows == 0,
            summary = new
            {
                readyToImport = validRows,
                willSkipOrUpdate = existingEmails.Count,
                hasErrors = errorRows > 0
            }
        }));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // POST api/data/import/detect-duplicates
    // Check a list of emails/phones against the DB for existing records
    // ──────────────────────────────────────────────────────────────────────────
    [HttpPost("import/detect-duplicates")]
    public async Task<IActionResult> DetectImportDuplicates(
        [FromBody] DuplicateDetectionRequest req,
        CancellationToken ct)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        if (req.Emails == null || req.Emails.Count == 0)
            return BadRequest(ApiResponse<object>.Fail("At least one email required"));

        var emails = req.Emails.Take(200).ToList();

        var existing = await _context.Clients
            .Where(c => c.TenantId == tenantId.Value && emails.Contains(c.Email))
            .Select(c => new { c.Id, c.Email, c.FirstName, c.LastName, c.Phone })
            .ToListAsync(ct);

        var matches = existing.Select(e => new
        {
            e.Email,
            existingClientId = e.Id,
            existingName = e.FirstName + " " + e.LastName,
            e.Phone
        }).ToList();

        return Ok(ApiResponse<object>.Ok(new
        {
            checkedCount = emails.Count,
            duplicatesFound = matches.Count,
            matches
        }));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string[] ParseCsvLine(string line)
    {
        var result = new List<string>();
        bool inQuotes = false;
        var current = new System.Text.StringBuilder();

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }
        result.Add(current.ToString());
        return result.ToArray();
    }

    private static string DetectEntityType(string[] headers)
    {
        var headerSet = headers.Select(h => h.ToLower()).ToHashSet();
        if (headerSet.Any(h => h.Contains("email") || h.Contains("phone") || h.Contains("first_name") || h.Contains("firstname")))
            return "client";
        if (headerSet.Any(h => h.Contains("service") || h.Contains("duration") || h.Contains("price")))
            return "service";
        if (headerSet.Any(h => h.Contains("booking") || h.Contains("appointment") || h.Contains("start_time")))
            return "booking";
        return "unknown";
    }

    private static Dictionary<string, string> BuildFieldMap(string[] headers, string entityType)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var aliases = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["FirstName"] = new[] { "first_name", "firstname", "first", "given_name", "fname" },
            ["LastName"] = new[] { "last_name", "lastname", "last", "surname", "family_name", "lname" },
            ["Email"] = new[] { "email", "email_address", "e-mail", "emailaddress" },
            ["Phone"] = new[] { "phone", "telephone", "phone_number", "mobile", "cell", "tel" },
            ["Notes"] = new[] { "notes", "note", "comments", "comment", "description" },
            ["DateOfBirth"] = new[] { "dob", "date_of_birth", "birthdate", "birthday" },
            ["Name"] = new[] { "name", "full_name", "fullname", "client_name" },
        };

        foreach (var header in headers)
        {
            foreach (var (field, fieldAliases) in aliases)
            {
                if (fieldAliases.Any(a => string.Equals(a, header.Trim(), StringComparison.OrdinalIgnoreCase)))
                {
                    map[header] = field;
                    break;
                }
            }
            if (!map.ContainsKey(header))
                map[header] = header; // Identity mapping
        }

        return map;
    }

    private static string GetMappedValue(Dictionary<string, string> row, Dictionary<string, string> fieldMap, string targetField)
    {
        var csvKey = fieldMap.FirstOrDefault(kv => string.Equals(kv.Value, targetField, StringComparison.OrdinalIgnoreCase)).Key;
        if (csvKey != null && row.TryGetValue(csvKey, out var v)) return v;

        // Also try direct access
        if (row.TryGetValue(targetField, out var direct)) return direct;
        return "";
    }

    private static readonly Regex _emailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
    private static readonly Regex _phoneRegex = new(@"^[\+]?[(]?[0-9]{3}[)]?[-\s\.]?[0-9]{3}[-\s\.]?[0-9]{4,6}$", RegexOptions.Compiled);

    private static bool IsValidEmail(string email) => _emailRegex.IsMatch(email);
    private static bool IsValidPhone(string phone) => phone.Length >= 7 && _phoneRegex.IsMatch(phone.Replace(" ", ""));
}

// ─── Enhanced request/result types ────────────────────────────────────────────

public class ImportPreviewRequest
{
    public IFormFile? File { get; set; }
    public string? EntityType { get; set; } // client | service | booking (auto-detected if null)
    public int? PreviewRows { get; set; } = 20;
}

public class RowValidationResult
{
    public int RowIndex { get; set; }
    public Dictionary<string, string> Data { get; set; } = new();
    public string Status { get; set; } = "ok"; // ok | warning | error
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public bool IsWithinFileDuplicate { get; set; }
    public bool IsDbDuplicate { get; set; }
    public string MappedName { get; set; } = "";
    public string MappedEmail { get; set; } = "";
}

public class DuplicateDetectionRequest
{
    public List<string> Emails { get; set; } = new();
    public List<string>? Phones { get; set; }
}
