using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.Infrastructure.Data;
using Upkilo.Core.Interfaces;
using Upkilo.Core.Entities;
using System.Text.Json;

namespace Upkilo.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class FormSubmissionsController : ControllerBase
{
    private readonly AppDbContext _context;

    public FormSubmissionsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("definitions/{definitionId}/submissions")]
    [Authorize]
    public async Task<IActionResult> GetSubmissions(Guid definitionId)
    {
        var submissions = await _context.FormSubmissions
            .Where(s => s.FormDefinitionId == definitionId)
            .OrderByDescending(s => s.SubmittedAt)
            .ToListAsync();
            
        return Ok(submissions);
    }

    [HttpPost("definitions/{definitionId}/submit")]
    [AllowAnonymous]
    public async Task<IActionResult> SubmitForm(Guid definitionId, [FromBody] JsonDocument responseData)
    {
        var form = await _context.FormDefinitions
            .Include(f => f.Fields)
            .FirstOrDefaultAsync(f => f.Id == definitionId && !f.IsDeleted);
            
        if (form == null || !form.IsActive) return NotFound("Form not found or inactive.");

        // Strict validation: check required fields
        var root = responseData.RootElement;
        foreach (var field in form.Fields.Where(f => f.IsRequired))
        {
            if (!root.TryGetProperty(field.Label, out var prop) || 
                prop.ValueKind == JsonValueKind.Null ||
                (prop.ValueKind == JsonValueKind.String && string.IsNullOrWhiteSpace(prop.GetString())))
            {
                return BadRequest(new { error = $"Field '{field.Label}' is required." });
            }
        }

        var submission = new FormSubmission
        {
            TenantId = form.TenantId,
            FormDefinitionId = definitionId,
            SubmittedAt = DateTime.UtcNow,
            ResponseDataJson = responseData.RootElement.ToString()
        };

        // If client is logged in, attach their ID
        if (User.Identity?.IsAuthenticated == true)
        {
            var userIdClm = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(userIdClm, out var clientId))
            {
                submission.SubmittedByClientId = clientId;
            }
        }

        _context.FormSubmissions.Add(submission);
        await _context.SaveChangesAsync();
        
        // Trigger workflow event: FormSubmitted
        
        return Ok(new { success = true, submissionId = submission.Id });
    }

    [HttpGet("definitions/{definitionId}/analytics")]
    [Authorize]
    public async Task<IActionResult> GetFormAnalytics(Guid definitionId)
    {
        var form = await _context.FormDefinitions.FindAsync(definitionId);
        if (form == null) return NotFound();

        var submissions = await _context.FormSubmissions
            .Where(s => s.FormDefinitionId == definitionId)
            .ToListAsync();

        var totalSubmissions = submissions.Count;
        var recentSubmissions = submissions.Count(s => s.SubmittedAt >= DateTime.UtcNow.AddDays(-30));
        var conversionRate = 0.0; // In a real app, track views vs submissions

        // Basic field completion stats could go here if parsed

        return Ok(new 
        { 
            definitionId, 
            totalSubmissions, 
            recentSubmissions,
            conversionRate
        });
    }
}
