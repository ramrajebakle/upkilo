using Upkilo.Core.Entities;

namespace Upkilo.Core.Interfaces;

/// <summary>
/// SMS service interface for sending SMS messages via Twilio
/// </summary>
public interface ISmsService
{
    /// <summary>
    /// Send an SMS message
    /// </summary>
    Task<SmsResult> SendSmsAsync(Guid tenantId, string toPhoneNumber, string message, Guid? clientId = null);

    /// <summary>
    /// Send a booking confirmation SMS
    /// </summary>
    Task<SmsResult> SendBookingConfirmationAsync(Booking booking);

    /// <summary>
    /// Send a booking reminder SMS
    /// </summary>
    Task<SmsResult> SendBookingReminderAsync(Booking booking);

    /// <summary>
    /// Send a booking cancellation SMS
    /// </summary>
    Task<SmsResult> SendBookingCancellationAsync(Booking booking);

    /// <summary>
    /// Send a 2FA verification code
    /// </summary>
    Task<SmsResult> SendVerificationCodeAsync(Guid tenantId, string phoneNumber, string code);
}

/// <summary>
/// Result of an SMS send operation
/// </summary>
public record SmsResult(
    bool Success,
    string? MessageId,
    string? Error
);

/// <summary>
/// Data for booking-related SMS messages (Keep for backward compatibility or specialized use)
/// </summary>
public record SmsBookingData(
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
