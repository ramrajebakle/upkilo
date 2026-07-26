using System;

namespace Upkilo.Core.Entities;

/// <summary>
/// SMS A2P 10DLC brand registration for compliance.
/// </summary>
public class SmsA2PBrand : TenantEntity
{
    public string BrandName { get; set; } = string.Empty;
    public string TcrBrandId { get; set; } = string.Empty;
    public string Ein { get; set; } = string.Empty; // Employer Identification Number
    public string VerticalType { get; set; } = string.Empty; // e.g. "Professional Services"
    public int TrustScore { get; set; } // 0-100
    public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
    public DateTime? ApprovedAt { get; set; }
}

/// <summary>
/// SMS A2P campaign registration.
/// </summary>
public class SmsCampaignRegistration : TenantEntity
{
    public Guid BrandId { get; set; }
    public string CampaignType { get; set; } = string.Empty; // Appointment Reminders, Marketing, etc.
    public string UseCase { get; set; } = string.Empty;
    public string SampleMessages { get; set; } = "[]"; // JSON array
    public string OptInWorkflow { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
    public int ThroughputLimit { get; set; } // Messages per second
    public virtual SmsA2PBrand? Brand { get; set; }
}

/// <summary>
/// SMS opt-in/opt-out tracking for compliance.
/// </summary>
public class SmsConsent : TenantEntity
{
    public Guid ClientId { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public bool IsOptedIn { get; set; } = true;
    public string ConsentMethod { get; set; } = "WebForm"; // WebForm, SMS, Import
    public DateTime ConsentedAt { get; set; } = DateTime.UtcNow;
    public DateTime? OptedOutAt { get; set; }
    public string? OptOutKeyword { get; set; } // STOP, CANCEL, etc.
}

/// <summary>
/// WhatsApp Cloud API configuration per tenant.
/// </summary>
public class WhatsAppConfig : TenantEntity
{
    public string PhoneNumberId { get; set; } = string.Empty;
    public string BusinessAccountId { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty; // Encrypted
    public bool IsVerified { get; set; }
    public string? WebhookVerifyToken { get; set; }
    public DateTime? VerifiedAt { get; set; }
}

/// <summary>
/// WhatsApp message template (pre-approved by Meta).
/// </summary>
public class WhatsAppTemplate : TenantEntity
{
    public string TemplateName { get; set; } = string.Empty;
    public string Category { get; set; } = "UTILITY"; // UTILITY, MARKETING, AUTHENTICATION
    public string Language { get; set; } = "en";
    public string HeaderType { get; set; } = "NONE"; // NONE, TEXT, IMAGE, VIDEO, DOCUMENT
    public string BodyText { get; set; } = string.Empty;
    public string? FooterText { get; set; }
    public string? Buttons { get; set; } // JSON
    public string Status { get; set; } = "PENDING"; // PENDING, APPROVED, REJECTED
    public string? MetaTemplateId { get; set; }
}

/// <summary>
/// Staff tip tracking and pooling.
/// </summary>
public class StaffTip : TenantEntity
{
    public Guid StaffMemberId { get; set; }
    public Guid? BookingId { get; set; }
    public Guid? ClientId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = "Card"; // Card, Cash
    public bool IsPooled { get; set; }
    public string? PoolDistribution { get; set; } // JSON: staff member shares
    public DateTime TipDate { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Waitlist auto-booking configuration and queue management.
/// </summary>
public class WaitlistConfig : TenantEntity
{
    public bool AutoBookEnabled { get; set; } = true;
    public int MaxWaitMinutes { get; set; } = 30;
    public int ExpiryHours { get; set; } = 24;
    public bool NotifyOnAvailability { get; set; } = true;
    public string NotificationChannel { get; set; } = "SMS"; // SMS, Email, Both
    public bool PriorityQueueEnabled { get; set; }
    public string? PriorityRules { get; set; } // JSON: VIP, loyalty tier, etc.
}
