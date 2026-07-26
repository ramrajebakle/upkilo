namespace Upkilo.Core.Entities;

/// <summary>
/// Persistent audit record of every government / law-enforcement data request.
/// Required for GDPR Art. 5(2) accountability, CCPA §1798.145, and internal transparency reporting.
/// </summary>
public class LegalDisclosureRequest : BaseEntity
{
    public string ReferenceNumber { get; set; } = string.Empty;

    /// <summary>Type of legal instrument: Subpoena, SearchWarrant, CourtOrder, NationalSecurityLetter, AdministrativeRequest, InformalRequest, Other</summary>
    public string RequestType { get; set; } = string.Empty;

    public string IssuingAuthority { get; set; } = string.Empty;
    public string IssuingJurisdiction { get; set; } = string.Empty;
    public string? StatutoryCitation { get; set; }

    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResponseDeadline { get; set; }

    /// <summary>Pending | UnderReview | Fulfilled | PartiallyFulfilled | Rejected | Challenged | Withdrawn</summary>
    public string Status { get; set; } = "Pending";

    public string? DataCategoriesRequested { get; set; }
    public string? DataCategoriesProvided { get; set; }
    public string? RejectionReason { get; set; }

    public bool UserNotified { get; set; }
    public DateTime? UserNotifiedAt { get; set; }
    public bool NotificationLegallyProhibited { get; set; }

    public Guid? ReviewedByUserId { get; set; }
    public string? LegalCounselNotes { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime? FulfilledAt { get; set; }
}
