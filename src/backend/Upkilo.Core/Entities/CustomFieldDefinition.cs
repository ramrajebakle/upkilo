using System;
using System.Collections.Generic;

namespace Upkilo.Core.Entities;

/// <summary>
/// Defines a custom field that a tenant can add to contacts, bookings, or invoices.
/// </summary>
public class CustomFieldDefinition : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public CustomFieldType FieldType { get; set; } = CustomFieldType.Text;
    public string EntityType { get; set; } = "Contact"; // Contact, Booking, Invoice
    public string TargetEntity { get; set; } = "Contact"; // Legacy compat — same as EntityType
    public bool IsRequired { get; set; }
    public bool IsSearchable { get; set; }
    public string? DefaultValue { get; set; }
    public string? ValidationRules { get; set; } // JSON: min, max, regex, etc.
    public string? Options { get; set; } // JSON array for Dropdown/MultiSelect/Radio
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual ICollection<CustomFieldValue> Values { get; set; } = new List<CustomFieldValue>();
}

/// <summary>
/// Stores the actual value of a custom field for a specific entity record.
/// </summary>
public class CustomFieldValue : TenantEntity
{
    public Guid CustomFieldDefinitionId { get; set; }
    public Guid EntityId { get; set; } // The ID of the Contact/Booking/Invoice
    public string EntityType { get; set; } = string.Empty;
    public string? TextValue { get; set; }
    public decimal? NumberValue { get; set; }
    public DateTime? DateValue { get; set; }
    public bool? BooleanValue { get; set; }
    public string? JsonValue { get; set; } // For complex types (file, address, etc.)

    public virtual CustomFieldDefinition? Definition { get; set; }
}

public enum CustomFieldType
{
    Text,
    Number,
    Date,
    DateTime,
    Dropdown,
    MultiSelect,
    Checkbox,
    Radio,
    TextArea,
    Email,
    Phone,
    Url
}
