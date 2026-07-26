namespace Upkilo.Core.Interfaces;

/// <summary>
/// WhatsApp service interface for sending messages via Twilio WhatsApp API.
/// </summary>
public interface IWhatsAppService
{
    /// <summary>
    /// Send a WhatsApp message.
    /// </summary>
    Task<WhatsAppResult> SendWhatsAppAsync(Guid tenantId, string toPhoneNumber, string message, Guid? clientId = null);

    /// <summary>
    /// Send a booking confirmation WhatsApp.
    /// </summary>
    Task<WhatsAppResult> SendBookingConfirmationAsync(WhatsAppBookingData data);

    /// <summary>
    /// Send a booking reminder WhatsApp.
    /// </summary>
    Task<WhatsAppResult> SendBookingReminderAsync(WhatsAppBookingData data);
}

/// <summary>
/// Result of a WhatsApp send operation.
/// </summary>
public record WhatsAppResult(
    bool Success,
    string? MessageId,
    string? Error
);

/// <summary>
/// Data for booking-related WhatsApp messages.
/// </summary>
public record WhatsAppBookingData(
    string PhoneNumber,
    Guid ClientId,
    Guid TenantId,
    string ClientName,
    string ServiceName,
    string StaffName,
    DateTime BookingDate,
    TimeSpan BookingTime,
    string BusinessName,
    string ConfirmationCode
);
