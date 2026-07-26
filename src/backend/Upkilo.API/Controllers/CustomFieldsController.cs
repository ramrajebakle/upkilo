using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Controllers;

/// <summary>
/// Custom fields API — define, manage, and query custom fields on contacts, bookings, and invoices.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/custom-fields")]
[Authorize]
public class CustomFieldsController : ControllerBase
{
    private readonly ILogger<CustomFieldsController> _logger;
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public CustomFieldsController(
        ILogger<CustomFieldsController> logger,
        AppDbContext context,
        ITenantProvider tenantProvider)
    {
        _logger = logger;
        _context = context;
        _tenantProvider = tenantProvider;
    }

    private static string NormalizeEntityType(string entityType)
    {
        if (string.IsNullOrWhiteSpace(entityType)) return string.Empty;
        var cleaned = entityType.Trim().ToLowerInvariant();
        if (cleaned.EndsWith("s")) cleaned = cleaned[..^1];
        return cleaned switch
        {
            "contact" => "Contact",
            "booking" => "Booking",
            "invoice" => "Invoice",
            "staff" => "Staff",
            _ => char.ToUpperInvariant(cleaned[0]) + cleaned[1..]
        };
    }

    /// <summary>
    /// Get all custom field definitions (optionally filtered by entity type)
    /// </summary>
    [HttpGet("")]
    [HttpGet("definitions")]
    public async Task<IActionResult> GetDefinitions(
        [FromQuery(Name = "entity_type")] string? entityType = null,
        [FromQuery(Name = "entityType")] string? entityTypeCamel = null)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var query = _context.CustomFieldDefinitions
            .Where(f => f.TenantId == tenantId.Value && !f.IsDeleted && f.IsActive);

        var actualEntityType = entityType ?? entityTypeCamel;
        if (!string.IsNullOrEmpty(actualEntityType))
        {
            actualEntityType = NormalizeEntityType(actualEntityType);
            query = query.Where(f => f.EntityType == actualEntityType);
        }

        var fields = await query
            .OrderBy(f => f.SortOrder)
            .Select(f => new
            {
                f.Id, f.Name, f.Label, f.FieldType, f.EntityType,
                f.IsRequired, f.IsSearchable, f.DefaultValue,
                f.ValidationRules, f.Options, f.SortOrder, f.CreatedAt
            })
            .ToListAsync();

        return Ok(new { data = fields });
    }

    /// <summary>
    /// Create a new custom field definition
    /// </summary>
    [HttpPost("")]
    [HttpPost("definitions")]
    public async Task<IActionResult> CreateDefinition([FromBody] CreateCustomFieldRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var normalizedEntityType = NormalizeEntityType(request.EntityType);

        var maxOrder = await _context.CustomFieldDefinitions
            .Where(f => f.TenantId == tenantId.Value && f.EntityType == normalizedEntityType && !f.IsDeleted)
            .MaxAsync(f => (int?)f.SortOrder) ?? 0;

        var field = new CustomFieldDefinition
        {
            TenantId = tenantId.Value,
            Name = request.Name.ToLower().Replace(" ", "_"),
            Label = request.Label,
            FieldType = request.FieldType,
            EntityType = normalizedEntityType,
            TargetEntity = normalizedEntityType,
            IsRequired = request.IsRequired,
            IsSearchable = request.IsSearchable,
            DefaultValue = request.DefaultValue,
            ValidationRules = request.ValidationRules,
            Options = request.Options,
            SortOrder = maxOrder + 1,
            IsActive = true
        };

        _context.CustomFieldDefinitions.Add(field);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Custom field created: {Name} for {EntityType}", field.Name, field.EntityType);
        return CreatedAtAction(nameof(GetDefinitions), new { id = field.Id }, field);
    }

    /// <summary>
    /// Update a custom field definition
    /// </summary>
    [HttpPut("{id}")]
    [HttpPut("definitions/{id}")]
    public async Task<IActionResult> UpdateDefinition(Guid id, [FromBody] UpdateCustomFieldRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var field = await _context.CustomFieldDefinitions
            .FirstOrDefaultAsync(f => f.Id == id && f.TenantId == tenantId.Value && !f.IsDeleted);
        if (field == null) return NotFound();

        if (request.Label != null) field.Label = request.Label;
        if (request.IsRequired.HasValue) field.IsRequired = request.IsRequired.Value;
        if (request.IsSearchable.HasValue) field.IsSearchable = request.IsSearchable.Value;
        if (request.DefaultValue != null) field.DefaultValue = request.DefaultValue;
        if (request.ValidationRules != null) field.ValidationRules = request.ValidationRules;
        if (request.Options != null) field.Options = request.Options;
        if (request.SortOrder.HasValue) field.SortOrder = request.SortOrder.Value;
        if (request.IsActive.HasValue) field.IsActive = request.IsActive.Value;

        field.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(field);
    }

    /// <summary>
    /// Delete a custom field definition (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    [HttpDelete("definitions/{id}")]
    public async Task<IActionResult> DeleteDefinition(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var field = await _context.CustomFieldDefinitions
            .FirstOrDefaultAsync(f => f.Id == id && f.TenantId == tenantId.Value && !f.IsDeleted);
        if (field == null) return NotFound();

        field.IsDeleted = true;
        field.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Get supported field types
    /// </summary>
    [HttpGet("field-types")]
    public IActionResult GetFieldTypes()
    {
        var types = Enum.GetValues<CustomFieldType>().Select(t => new
        {
            id = t.ToString(),
            name = t switch
            {
                CustomFieldType.Text => "Single Line Text",
                CustomFieldType.Number => "Number",
                CustomFieldType.Date => "Date",
                CustomFieldType.DateTime => "Date & Time",
                CustomFieldType.Dropdown => "Dropdown Select",
                CustomFieldType.MultiSelect => "Multi-Select",
                CustomFieldType.Checkbox => "Checkbox",
                CustomFieldType.Radio => "Radio Buttons",
                CustomFieldType.TextArea => "Multi-Line Text",
                CustomFieldType.Email => "Email",
                CustomFieldType.Phone => "Phone Number",
                CustomFieldType.Url => "URL",
                _ => t.ToString()
            }
        });

        return Ok(new { data = types });
    }

    /// <summary>
    /// Set custom field values for an entity
    /// </summary>
    [HttpPost("values")]
    public async Task<IActionResult> SetValues([FromBody] SetCustomFieldValuesRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        foreach (var entry in request.Values)
        {
            var existing = await _context.CustomFieldValues
                .FirstOrDefaultAsync(v =>
                    v.CustomFieldDefinitionId == entry.FieldId &&
                    v.EntityId == request.EntityId &&
                    v.TenantId == tenantId.Value &&
                    !v.IsDeleted);

            if (existing != null)
            {
                existing.TextValue = entry.TextValue;
                existing.NumberValue = entry.NumberValue;
                existing.DateValue = entry.DateValue;
                existing.BooleanValue = entry.BooleanValue;
                existing.JsonValue = entry.JsonValue;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _context.CustomFieldValues.Add(new CustomFieldValue
                {
                    TenantId = tenantId.Value,
                    CustomFieldDefinitionId = entry.FieldId,
                    EntityId = request.EntityId,
                    EntityType = request.EntityType,
                    TextValue = entry.TextValue,
                    NumberValue = entry.NumberValue,
                    DateValue = entry.DateValue,
                    BooleanValue = entry.BooleanValue,
                    JsonValue = entry.JsonValue
                });
            }
        }

        await _context.SaveChangesAsync();
        return Ok(new { success = true, valuesSet = request.Values.Count });
    }

    /// <summary>
    /// Get custom field values for an entity
    /// </summary>
    [HttpGet("values/{entityType}/{entityId}")]
    public async Task<IActionResult> GetValues(string entityType, Guid entityId)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var values = await _context.CustomFieldValues
            .Include(v => v.Definition)
            .Where(v => v.EntityId == entityId && v.EntityType == entityType &&
                        v.TenantId == tenantId.Value && !v.IsDeleted)
            .Select(v => new
            {
                v.Id,
                v.CustomFieldDefinitionId,
                fieldName = v.Definition != null ? v.Definition.Name : null,
                fieldLabel = v.Definition != null ? v.Definition.Label : null,
                fieldType = v.Definition != null ? v.Definition.FieldType.ToString() : null,
                v.TextValue, v.NumberValue, v.DateValue, v.BooleanValue, v.JsonValue
            })
            .ToListAsync();

        return Ok(new { data = values });
    }

    /// <summary>
    /// Search entities by custom field values
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> SearchByCustomField(
        [FromQuery] string entityType,
        [FromQuery] Guid fieldId,
        [FromQuery] string query,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var field = await _context.CustomFieldDefinitions
            .FirstOrDefaultAsync(f => f.Id == fieldId && f.TenantId == tenantId.Value && f.IsSearchable);

        if (field == null)
            return BadRequest(new { error = "Field not found or not searchable." });

        var matchingValues = await _context.CustomFieldValues
            .Where(v => v.CustomFieldDefinitionId == fieldId && v.EntityType == entityType &&
                        v.TenantId == tenantId.Value && !v.IsDeleted &&
                        (v.TextValue != null && v.TextValue.Contains(query)))
            .OrderByDescending(v => v.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(v => new { v.EntityId, v.TextValue, v.NumberValue, v.DateValue, v.BooleanValue })
            .ToListAsync();

        return Ok(new { data = matchingValues, fieldName = field.Name });
    }

    /// <summary>
    /// Get custom field values for a specific contact
    /// </summary>
    [HttpGet("~/api/v{version:apiVersion}/contacts/{id}/custom-fields")]
    public async Task<IActionResult> GetContactValues(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        // Verify contact exists and belongs to tenant
        var contactExists = await _context.Clients.AnyAsync(c => c.Id == id && c.TenantId == tenantId.Value);
        if (!contactExists) return NotFound("Contact not found.");

        var values = await _context.CustomFieldValues
            .Include(v => v.Definition)
            .Where(v => v.EntityId == id && v.EntityType == "Contact" &&
                        v.TenantId == tenantId.Value && !v.IsDeleted)
            .Select(v => new
            {
                v.Id,
                v.CustomFieldDefinitionId,
                fieldName = v.Definition != null ? v.Definition.Name : null,
                fieldLabel = v.Definition != null ? v.Definition.Label : null,
                fieldType = v.Definition != null ? v.Definition.FieldType.ToString() : null,
                v.TextValue, v.NumberValue, v.DateValue, v.BooleanValue, v.JsonValue
            })
            .ToListAsync();

        return Ok(new { data = values });
    }

    /// <summary>
    /// Set custom field values for a specific contact
    /// </summary>
    [HttpPut("~/api/v{version:apiVersion}/contacts/{id}/custom-fields")]
    public async Task<IActionResult> SetContactValues(Guid id, [FromBody] SetContactCustomFieldValuesRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        // Verify contact exists and belongs to tenant
        var contactExists = await _context.Clients.AnyAsync(c => c.Id == id && c.TenantId == tenantId.Value);
        if (!contactExists) return NotFound("Contact not found.");

        foreach (var entry in request.Values)
        {
            // Verify definition exists, is for "Contact", and belongs to tenant
            var definition = await _context.CustomFieldDefinitions
                .FirstOrDefaultAsync(d => d.Id == entry.FieldId && d.TenantId == tenantId.Value && d.EntityType == "Contact" && !d.IsDeleted);
            if (definition == null)
            {
                return BadRequest($"Custom field definition not found or not for Contact: {entry.FieldId}");
            }

            var existing = await _context.CustomFieldValues
                .FirstOrDefaultAsync(v =>
                    v.CustomFieldDefinitionId == entry.FieldId &&
                    v.EntityId == id &&
                    v.TenantId == tenantId.Value &&
                    !v.IsDeleted);

            if (existing != null)
            {
                existing.TextValue = entry.TextValue;
                existing.NumberValue = entry.NumberValue;
                existing.DateValue = entry.DateValue;
                existing.BooleanValue = entry.BooleanValue;
                existing.JsonValue = entry.JsonValue;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _context.CustomFieldValues.Add(new CustomFieldValue
                {
                    TenantId = tenantId.Value,
                    CustomFieldDefinitionId = entry.FieldId,
                    EntityId = id,
                    EntityType = "Contact",
                    TextValue = entry.TextValue,
                    NumberValue = entry.NumberValue,
                    DateValue = entry.DateValue,
                    BooleanValue = entry.BooleanValue,
                    JsonValue = entry.JsonValue
                });
            }
        }

        await _context.SaveChangesAsync();
        return Ok(new { success = true, valuesSet = request.Values.Count });
    }
}

// ── DTOs ──────────────────────────────────────────────────────────────────────

public class CreateCustomFieldRequest
{
    public string Name { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public CustomFieldType FieldType { get; set; }
    public string EntityType { get; set; } = "Contact"; // Contact, Booking, Invoice
    public bool IsRequired { get; set; }
    public bool IsSearchable { get; set; }
    public string? DefaultValue { get; set; }
    public string? ValidationRules { get; set; }
    public string? Options { get; set; }
}

public class UpdateCustomFieldRequest
{
    public string? Label { get; set; }
    public bool? IsRequired { get; set; }
    public bool? IsSearchable { get; set; }
    public string? DefaultValue { get; set; }
    public string? ValidationRules { get; set; }
    public string? Options { get; set; }
    public int? SortOrder { get; set; }
    public bool? IsActive { get; set; }
}

public class SetCustomFieldValuesRequest
{
    public Guid EntityId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public List<CustomFieldValueEntry> Values { get; set; } = new();
}

public class CustomFieldValueEntry
{
    public Guid FieldId { get; set; }
    public string? TextValue { get; set; }
    public decimal? NumberValue { get; set; }
    public DateTime? DateValue { get; set; }
    public bool? BooleanValue { get; set; }
    public string? JsonValue { get; set; }
}

public class SetContactCustomFieldValuesRequest
{
    public List<CustomFieldValueEntry> Values { get; set; } = new();
}
