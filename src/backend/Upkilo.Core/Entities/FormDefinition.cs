using System;
using System.Collections.Generic;

namespace Upkilo.Core.Entities;

/// <summary>
/// A form definition created by the tenant (intake forms, surveys, lead capture).
/// </summary>
public class FormDefinition : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = "Draft"; // Draft, Published, Archived
    public bool IsActive { get; set; } = true;
    public bool IsMultiStep { get; set; }
    public int TotalSteps { get; set; } = 1;
    public string? ConditionalLogic { get; set; } // JSON rules for show/hide
    public string? CustomCss { get; set; }
    public string? EmbedCode { get; set; }
    public string? RedirectUrl { get; set; }
    public string? SuccessMessage { get; set; } = "Thank you for your submission!";
    public int SubmissionCount { get; set; }
    public int ViewCount { get; set; }

    public virtual ICollection<FormField> Fields { get; set; } = new List<FormField>();
    public virtual ICollection<FormSubmission> Submissions { get; set; } = new List<FormSubmission>();
}

/// <summary>
/// Individual field within a form.
/// </summary>
public class FormField : TenantEntity
{
    public Guid FormDefinitionId { get; set; }
    public string Label { get; set; } = string.Empty;
    public FormFieldType FieldType { get; set; } = FormFieldType.Text;
    public string? Placeholder { get; set; }
    public bool IsRequired { get; set; }
    public string? ValidationRules { get; set; } // JSON
    public string? Options { get; set; } // JSON for dropdowns/radios
    public string? OptionsJson { get; set; }
    public int StepNumber { get; set; } = 1;
    public int SortOrder { get; set; }
    public int OrderIndex { get; set; }
    public string? ConditionalVisibility { get; set; } // JSON: show if field X = value Y

    public virtual FormDefinition? Form { get; set; }
}

/// <summary>
/// A submitted form response.
/// </summary>
public class FormSubmission : TenantEntity
{
    public Guid FormDefinitionId { get; set; }
    public string Data { get; set; } = "{}"; // JSON of all field values
    public string? ResponseDataJson { get; set; }
    public Guid? SubmittedByClientId { get; set; }
    public string? SubmitterEmail { get; set; }
    public string? SubmitterName { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Source { get; set; } // utm_source
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    public virtual FormDefinition? Form { get; set; }
}

public enum FormFieldType
{
    Text,
    Email,
    Phone,
    Number,
    Date,
    DateTime,
    TextArea,
    Dropdown,
    MultiSelect,
    Checkbox,
    Radio,
    FileUpload,
    Signature,
    Address,
    Rating,
    Hidden,
    Heading,
    Paragraph,
    Divider,
    Spacer
}
