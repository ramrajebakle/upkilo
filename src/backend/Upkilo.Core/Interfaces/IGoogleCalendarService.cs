using Upkilo.Core.Entities;

namespace Upkilo.Core.Interfaces;

public interface IGoogleCalendarService : ICalendarService
{
    /// <summary>
    /// Refreshes the access token using the stored refresh token.
    /// </summary>
    Task<(string AccessToken, DateTime ExpiresAt)> RefreshAccessTokenAsync(string refreshToken);

    /// <summary>
    /// Pushes a single booking event to the user's Google Calendar.
    /// </summary>
    Task<string> PushBookingAsync(string accessToken, Booking booking, string? existingEventId = null);

    /// <summary>
    /// Deletes a booking event from the user's Google Calendar.
    /// </summary>
    Task DeleteBookingAsync(string accessToken, string eventId);

    /// <summary>
    /// Pulls all events from Google Calendar that are not Upkilo bookings (for block-offs).
    /// </summary>
    Task<IEnumerable<GoogleCalendarEvent>> PullEventsAsync(string accessToken, DateTime from, DateTime to);
}

public class GoogleCalendarEvent
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Status { get; set; } = string.Empty;
}
