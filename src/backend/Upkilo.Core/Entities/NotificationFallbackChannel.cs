namespace Upkilo.Core.Entities;

/// <summary>
/// Configure fallback channels if primary notification delivery fails
/// e.g. Try Push -> if failed -> Try Email -> if failed -> Try SMS
/// </summary>
public class NotificationFallbackChannel : TenantEntity
{
    public Guid UserId { get; set; }
    
    public string NotificationType { get; set; } = string.Empty; // e.g., "BookingConfirmation", "BillingAlert"
    
    public string PrimaryChannel { get; set; } = "Push"; // Push, Email, SMS, WhatsApp
    
    public string FirstFallbackChannel { get; set; } = "Email"; 
    
    public string? SecondFallbackChannel { get; set; } 
    
    public int TimeoutSecondsBeforeFallback { get; set; } = 300; // 5 minutes standard
    
    public bool IsActive { get; set; } = true;
    
    // Navigation
    public virtual User? User { get; set; }
}
