using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Controllers;

/// <summary>
/// Forms controller for custom intake and consent forms.
/// All endpoints use real database queries against FormDefinitions, FormFields, and FormSubmissions.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class FormsController : ControllerBase
{
    private readonly ILogger<FormsController> _logger;
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public FormsController(
        ILogger<FormsController> logger,
        AppDbContext context,
        ITenantProvider tenantProvider)
    {
        _logger = logger;
        _context = context;
        _tenantProvider = tenantProvider;
    }

    /// <summary>
    /// Get all form definitions for the current tenant
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetForms(
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var query = _context.FormDefinitions
            .Where(f => f.TenantId == tenantId.Value && !f.IsDeleted);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(f => f.Status == status);

        var total = await query.CountAsync();

        var forms = await query
            .OrderByDescending(f => f.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(f => new
            {
                f.Id,
                f.Name,
                f.Description,
                f.Status,
                f.IsMultiStep,
                f.TotalSteps,
                fieldCount = f.Fields.Count(ff => !ff.IsDeleted),
                f.SubmissionCount,
                f.ViewCount,
                f.CreatedAt,
                f.UpdatedAt
            })
            .ToListAsync();

        return Ok(new { data = forms, total, page, limit });
    }

    /// <summary>
    /// Get form definition by ID, including all fields
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetForm(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var form = await _context.FormDefinitions
            .Include(f => f.Fields.Where(ff => !ff.IsDeleted).OrderBy(ff => ff.SortOrder))
            .FirstOrDefaultAsync(f => f.Id == id && f.TenantId == tenantId.Value && !f.IsDeleted);

        if (form == null) return NotFound();

        return Ok(new
        {
            form.Id,
            form.Name,
            form.Description,
            form.Status,
            form.IsMultiStep,
            form.TotalSteps,
            form.ConditionalLogic,
            form.CustomCss,
            form.EmbedCode,
            form.RedirectUrl,
            form.SuccessMessage,
            form.SubmissionCount,
            form.ViewCount,
            fields = form.Fields.Select(f => new
            {
                f.Id,
                f.Label,
                f.FieldType,
                f.Placeholder,
                f.IsRequired,
                f.ValidationRules,
                f.Options,
                f.StepNumber,
                f.SortOrder,
                f.ConditionalVisibility
            }),
            form.CreatedAt,
            form.UpdatedAt
        });
    }

    /// <summary>
    /// Create a new form definition with fields
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateForm([FromBody] CreateFormRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { error = "Form name is required." });

        var form = new FormDefinition
        {
            TenantId = tenantId.Value,
            Name = request.Name,
            Description = request.Description,
            Status = "Draft",
            IsMultiStep = request.Fields?.Any(f => f.StepNumber > 1) ?? false,
            TotalSteps = request.Fields?.Max(f => f.StepNumber) ?? 1,
            SuccessMessage = "Thank you for your submission!",
            RedirectUrl = request.RedirectUrl
        };

        _context.FormDefinitions.Add(form);

        // Add fields
        if (request.Fields != null)
        {
            var order = 0;
            foreach (var field in request.Fields)
            {
                _context.FormFields.Add(new FormField
                {
                    TenantId = tenantId.Value,
                    FormDefinitionId = form.Id,
                    Label = field.Label,
                    FieldType = field.FieldType,
                    Placeholder = field.Placeholder,
                    IsRequired = field.IsRequired,
                    ValidationRules = field.ValidationRules,
                    Options = field.Options,
                    StepNumber = field.StepNumber,
                    SortOrder = order++,
                    ConditionalVisibility = field.ConditionalVisibility
                });
            }
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Form created: {FormId} - {Name} for tenant {TenantId}",
            form.Id, form.Name, tenantId);

        return CreatedAtAction(nameof(GetForm), new { id = form.Id }, new { form.Id, form.Name, form.Status, form.CreatedAt });
    }

    /// <summary>
    /// Update a form definition (name, description, status, fields)
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateForm(Guid id, [FromBody] UpdateFormRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var form = await _context.FormDefinitions
            .FirstOrDefaultAsync(f => f.Id == id && f.TenantId == tenantId.Value && !f.IsDeleted);

        if (form == null) return NotFound();

        if (request.Name != null) form.Name = request.Name;
        if (request.Description != null) form.Description = request.Description;
        if (request.Status != null) form.Status = request.Status;
        if (request.SuccessMessage != null) form.SuccessMessage = request.SuccessMessage;
        if (request.RedirectUrl != null) form.RedirectUrl = request.RedirectUrl;
        // WL-14: same CSS sanitization as WhiteLabelController — no external url(), @import, etc.
        if (request.CustomCss != null)
        {
            try { form.CustomCss = Upkilo.API.Infrastructure.BrandingValidator.SanitizeCss(request.CustomCss); }
            catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
        }
        if (request.ConditionalLogic != null) form.ConditionalLogic = request.ConditionalLogic;
        form.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Form updated: {FormId}", id);

        return Ok(new { form.Id, form.Name, form.Status, form.UpdatedAt });
    }

    /// <summary>
    /// Soft-delete a form definition
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteForm(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var form = await _context.FormDefinitions
            .FirstOrDefaultAsync(f => f.Id == id && f.TenantId == tenantId.Value && !f.IsDeleted);

        if (form == null) return NotFound();

        form.IsDeleted = true;
        form.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Form soft-deleted: {FormId}", id);
        return NoContent();
    }

    /// <summary>
    /// Duplicate a form and all its fields
    /// </summary>
    [HttpPost("{id}/duplicate")]
    public async Task<IActionResult> DuplicateForm(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var original = await _context.FormDefinitions
            .Include(f => f.Fields.Where(ff => !ff.IsDeleted))
            .FirstOrDefaultAsync(f => f.Id == id && f.TenantId == tenantId.Value && !f.IsDeleted);

        if (original == null) return NotFound();

        var copy = new FormDefinition
        {
            TenantId = tenantId.Value,
            Name = $"{original.Name} (Copy)",
            Description = original.Description,
            Status = "Draft",
            IsMultiStep = original.IsMultiStep,
            TotalSteps = original.TotalSteps,
            ConditionalLogic = original.ConditionalLogic,
            CustomCss = original.CustomCss,
            SuccessMessage = original.SuccessMessage,
            RedirectUrl = original.RedirectUrl
        };

        _context.FormDefinitions.Add(copy);

        foreach (var field in original.Fields)
        {
            _context.FormFields.Add(new FormField
            {
                TenantId = tenantId.Value,
                FormDefinitionId = copy.Id,
                Label = field.Label,
                FieldType = field.FieldType,
                Placeholder = field.Placeholder,
                IsRequired = field.IsRequired,
                ValidationRules = field.ValidationRules,
                Options = field.Options,
                StepNumber = field.StepNumber,
                SortOrder = field.SortOrder,
                ConditionalVisibility = field.ConditionalVisibility
            });
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Form duplicated: {OriginalId} -> {NewId}", id, copy.Id);

        return Ok(new { copy.Id, copy.Name, copy.Status, copy.CreatedAt });
    }

    /// <summary>
    /// Get form submissions with pagination
    /// </summary>
    [HttpGet("{id}/submissions")]
    public async Task<IActionResult> GetSubmissions(Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var query = _context.FormSubmissions
            .Where(s => s.FormDefinitionId == id && s.TenantId == tenantId.Value && !s.IsDeleted);

        var total = await query.CountAsync();

        var submissions = await query
            .OrderByDescending(s => s.SubmittedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new
            {
                s.Id,
                s.FormDefinitionId,
                s.SubmitterName,
                s.SubmitterEmail,
                s.Data,
                s.IpAddress,
                s.Source,
                s.SubmittedAt,
                s.CreatedAt
            })
            .ToListAsync();

        return Ok(new { data = submissions, total, page, pageSize });
    }

    /// <summary>
    /// Get a specific submission by ID
    /// </summary>
    [HttpGet("submissions/{submissionId}")]
    public async Task<IActionResult> GetSubmission(Guid submissionId)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var submission = await _context.FormSubmissions
            .Include(s => s.Form)
            .FirstOrDefaultAsync(s => s.Id == submissionId && s.TenantId == tenantId.Value && !s.IsDeleted);

        if (submission == null) return NotFound();

        return Ok(new
        {
            submission.Id,
            submission.FormDefinitionId,
            formName = submission.Form?.Name,
            submission.SubmitterName,
            submission.SubmitterEmail,
            submission.Data,
            submission.IpAddress,
            submission.UserAgent,
            submission.Source,
            submission.SubmittedAt,
            submission.CreatedAt
        });
    }

    /// <summary>
    /// Submit a form (public endpoint for clients)
    /// </summary>
    [HttpPost("{id}/submit")]
    [AllowAnonymous]
    public async Task<IActionResult> SubmitForm(Guid id, [FromBody] SubmitFormRequest request)
    {
        // We need the form's TenantId since this is anonymous
        var form = await _context.FormDefinitions
            .FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted && f.Status == "Published");

        if (form == null)
            return NotFound(new { error = "Form not found or not published." });

        var submission = new FormSubmission
        {
            TenantId = form.TenantId,
            FormDefinitionId = id,
            Data = JsonSerializer.Serialize(request.Data),
            SubmitterEmail = request.SubmitterEmail,
            SubmitterName = request.SubmitterName,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString(),
            Source = request.Source,
            SubmittedAt = DateTime.UtcNow
        };

        _context.FormSubmissions.Add(submission);

        // Increment submission count on the form
        form.SubmissionCount++;
        form.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Form submitted: {FormId}, Submission: {SubmissionId}", id, submission.Id);

        return Ok(new
        {
            submissionId = submission.Id,
            success = true,
            message = form.SuccessMessage ?? "Thank you for your submission!"
        });
    }

    /// <summary>
    /// Export form submissions to CSV
    /// </summary>
    [HttpGet("{id}/export")]
    public async Task<IActionResult> ExportSubmissions(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var form = await _context.FormDefinitions
            .Include(f => f.Fields.Where(ff => !ff.IsDeleted).OrderBy(ff => ff.SortOrder))
            .FirstOrDefaultAsync(f => f.Id == id && f.TenantId == tenantId.Value && !f.IsDeleted);

        if (form == null) return NotFound();

        var submissions = await _context.FormSubmissions
            .Where(s => s.FormDefinitionId == id && s.TenantId == tenantId.Value && !s.IsDeleted)
            .OrderByDescending(s => s.SubmittedAt)
            .Take(10000) // Safety limit
            .ToListAsync();

        var fieldLabels = form.Fields.Select(f => f.Label).ToList();
        var sb = new System.Text.StringBuilder();

        // Header
        sb.AppendLine(string.Join(",", new[] { "Submitter", "Email", "Submitted At" }.Concat(fieldLabels)));

        // Data rows
        foreach (var sub in submissions)
        {
            var values = new List<string>
            {
                EscapeCsv(sub.SubmitterName ?? ""),
                EscapeCsv(sub.SubmitterEmail ?? ""),
                sub.SubmittedAt.ToString("o")
            };

            // Parse submission data JSON
            try
            {
                var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(sub.Data);
                foreach (var label in fieldLabels)
                {
                    if (data != null && data.TryGetValue(label, out var val))
                        values.Add(EscapeCsv(val.ToString()));
                    else
                        values.Add("");
                }
            }
            catch
            {
                values.AddRange(fieldLabels.Select(_ => ""));
            }

            sb.AppendLine(string.Join(",", values));
        }

        return File(System.Text.Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"form_{id}_submissions.csv");
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    /// <summary>
    /// Generate embed code for a form
    /// </summary>
    [HttpGet("{id}/embed-code")]
    public async Task<IActionResult> GetEmbedCode(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var form = await _context.FormDefinitions
            .FirstOrDefaultAsync(f => f.Id == id && f.TenantId == tenantId.Value && !f.IsDeleted);
        if (form == null) return NotFound();

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var embedHtml = $@"<!-- Upkilo Form Embed -->
<div id=""upkilo-form-{id}""></div>
<script src=""{baseUrl}/embed/forms.js""></script>
<script>UpkiloForms.render('{id}', {{ container: '#upkilo-form-{id}' }});</script>";

        var iframeEmbed = $@"<iframe src=""{baseUrl}/forms/embed/{id}"" width=""100%"" height=""600"" frameborder=""0"" style=""border:none;""></iframe>";

        // Update the form's embed code
        form.EmbedCode = embedHtml;
        await _context.SaveChangesAsync();

        return Ok(new
        {
            formId = id,
            formName = form.Name,
            embedHtml,
            iframeEmbed,
            directLink = $"{baseUrl}/forms/{id}/submit"
        });
    }

    /// <summary>
    /// Get form analytics (views, submissions, conversion rate, trends)
    /// </summary>
    [HttpGet("{id}/analytics")]
    public async Task<IActionResult> GetFormAnalytics(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var form = await _context.FormDefinitions
            .FirstOrDefaultAsync(f => f.Id == id && f.TenantId == tenantId.Value && !f.IsDeleted);
        if (form == null) return NotFound();

        var submissions = await _context.FormSubmissions
            .Where(s => s.FormDefinitionId == id && s.TenantId == tenantId.Value && !s.IsDeleted)
            .ToListAsync();

        var last30Days = DateTime.UtcNow.AddDays(-30);
        var recentSubmissions = submissions.Where(s => s.SubmittedAt >= last30Days).ToList();

        var dailyTrend = recentSubmissions
            .GroupBy(s => s.SubmittedAt.Date)
            .OrderBy(g => g.Key)
            .Select(g => new { date = g.Key.ToString("yyyy-MM-dd"), count = g.Count() })
            .ToList();

        var sourcesBreakdown = submissions
            .GroupBy(s => s.Source ?? "Direct")
            .Select(g => new { source = g.Key, count = g.Count() })
            .OrderByDescending(g => g.count)
            .ToList();

        return Ok(new
        {
            formId = id,
            formName = form.Name,
            totalViews = form.ViewCount,
            totalSubmissions = form.SubmissionCount,
            conversionRate = form.ViewCount > 0 ? Math.Round((double)form.SubmissionCount / form.ViewCount * 100, 1) : 0,
            last30DaysSubmissions = recentSubmissions.Count,
            dailyTrend,
            sourcesBreakdown,
            avgSubmissionsPerDay = recentSubmissions.Count > 0 ? Math.Round((double)recentSubmissions.Count / 30, 1) : 0
        });
    }

    /// <summary>
    /// Get available field types for form builder
    /// </summary>
    [HttpGet("field-types")]
    public IActionResult GetFieldTypes()
    {
        var fieldTypes = new List<object>
        {
            new { id = "Text", name = "Single Line Text", icon = "type", category = "Basic" },
            new { id = "TextArea", name = "Multi-Line Text", icon = "align-left", category = "Basic" },
            new { id = "Email", name = "Email Address", icon = "mail", category = "Basic" },
            new { id = "Phone", name = "Phone Number", icon = "phone", category = "Basic" },
            new { id = "Number", name = "Number", icon = "hash", category = "Basic" },
            new { id = "Url", name = "Website URL", icon = "link", category = "Basic" },
            new { id = "Date", name = "Date Picker", icon = "calendar", category = "Date/Time" },
            new { id = "DateTime", name = "Date & Time", icon = "clock", category = "Date/Time" },
            new { id = "Time", name = "Time Only", icon = "clock", category = "Date/Time" },
            new { id = "Dropdown", name = "Dropdown Select", icon = "chevron-down", category = "Choice" },
            new { id = "MultiSelect", name = "Multi-Select", icon = "check-square", category = "Choice" },
            new { id = "Checkbox", name = "Checkbox", icon = "check", category = "Choice" },
            new { id = "Radio", name = "Radio Buttons", icon = "circle", category = "Choice" },
            new { id = "FileUpload", name = "File Upload", icon = "upload", category = "Advanced" },
            new { id = "Signature", name = "Signature Pad", icon = "edit", category = "Advanced" },
            new { id = "Rating", name = "Star Rating", icon = "star", category = "Advanced" },
            new { id = "Hidden", name = "Hidden Field", icon = "eye-off", category = "Advanced" },
            new { id = "Heading", name = "Section Heading", icon = "heading", category = "Layout" },
            new { id = "Divider", name = "Divider Line", icon = "minus", category = "Layout" },
            new { id = "Paragraph", name = "Info Paragraph", icon = "file-text", category = "Layout" }
        };

        return Ok(new { data = fieldTypes });
    }
}

// ── Request DTOs ──────────────────────────────────────────────────────────────

public class CreateFormRequest
{
    public string Name { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? RedirectUrl { get; set; }
    public List<CreateFormFieldRequest>? Fields { get; set; }
}

public class CreateFormFieldRequest
{
    public string Label { get; set; } = string.Empty;
    public FormFieldType FieldType { get; set; } = FormFieldType.Text;
    public string? Placeholder { get; set; }
    public bool IsRequired { get; set; }
    public string? ValidationRules { get; set; }
    public string? Options { get; set; }
    public string? OptionsJson { get; set; }
    public int StepNumber { get; set; } = 1;
    public int SortOrder { get; set; }
    public int OrderIndex { get; set; }
    public string? ConditionalVisibility { get; set; }
}

public class UpdateFormRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Status { get; set; }
    public string? SuccessMessage { get; set; }
    public string? RedirectUrl { get; set; }
    public string? CustomCss { get; set; }
    public string? ConditionalLogic { get; set; }
}

public class SubmitFormRequest
{
    public string? SubmitterName { get; set; }
    public string? SubmitterEmail { get; set; }
    public string? Source { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
}

