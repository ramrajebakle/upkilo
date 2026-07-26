using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.Infrastructure.Data;
using Upkilo.Core.Interfaces;
using Upkilo.Core.Entities;

namespace Upkilo.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class FormDefinitionsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public FormDefinitionsController(AppDbContext context, ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    [HttpGet]
    public async Task<IActionResult> GetForms()
    {
        var tenantId = _tenantProvider.GetTenantId();
        var forms = await _context.FormDefinitions
            .Include(f => f.Fields)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();
            
        return Ok(forms);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetForm(Guid id)
    {
        var form = await _context.FormDefinitions
            .Include(f => f.Fields)
            .FirstOrDefaultAsync(f => f.Id == id);
            
        if (form == null) return NotFound();
        return Ok(form);
    }

    [HttpPost]
    public async Task<IActionResult> CreateForm([FromBody] CreateFormRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var form = new FormDefinition
        {
            TenantId = tenantId.Value,
            Title = request.Title,
            Description = request.Description,
            IsActive = true
        };

        foreach (var field in request.Fields ?? new List<CreateFormFieldRequest>())
        {
            form.Fields.Add(new FormField
            {
                TenantId = tenantId.Value,
                Label = field.Label,
                FieldType = field.FieldType,
                IsRequired = field.IsRequired,
                OrderIndex = field.OrderIndex,
                OptionsJson = field.OptionsJson ?? "[]"
            });
        }

        _context.FormDefinitions.Add(form);
        await _context.SaveChangesAsync();
        
        return CreatedAtAction(nameof(GetForm), new { id = form.Id }, form);
    }

    /// <summary>
    /// Clone an existing form definition
    /// </summary>
    [HttpPost("{id}/clone")]
    public async Task<IActionResult> CloneForm(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var form = await _context.FormDefinitions
            .Include(f => f.Fields)
            .FirstOrDefaultAsync(f => f.Id == id && f.TenantId == tenantId.Value && !f.IsDeleted);

        if (form == null) return NotFound();

        var clone = new FormDefinition
        {
            TenantId = tenantId.Value,
            Title = $"{form.Title} (Copy)",
            Description = form.Description,
            IsActive = false // Default cloned form to Draft/Inactive
        };

        foreach (var field in form.Fields)
        {
            clone.Fields.Add(new FormField
            {
                TenantId = tenantId.Value,
                Label = field.Label,
                FieldType = field.FieldType,
                IsRequired = field.IsRequired,
                OrderIndex = field.OrderIndex,
                OptionsJson = field.OptionsJson
            });
        }

        _context.FormDefinitions.Add(clone);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetForm), new { id = clone.Id }, clone);
    }

    /// <summary>
    /// Archive a form definition (soft delete)
    /// </summary>
    [HttpPost("{id}/archive")]
    public async Task<IActionResult> ArchiveForm(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var form = await _context.FormDefinitions
            .FirstOrDefaultAsync(f => f.Id == id && f.TenantId == tenantId.Value && !f.IsDeleted);

        if (form == null) return NotFound();

        form.IsActive = false;
        form.IsDeleted = true;
        form.DeletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "Form archived successfully." });
    }
}
