using Upkilo.Core.Entities;

namespace Upkilo.Core.Interfaces;

public interface ICalendarService
{
    /// <summary>
    /// Gets the authorization URL for the provider
    /// </summary>
    string GetAuthUrl(string provider, Guid staffId);

    /// <summary>
    /// Exchanges authorization code for tokens
    /// </summary>
    Task<CalendarSyncToken> ConnectAsync(string provider, Guid staffId, string code);

    /// <summary>
    /// Syncs bookings between Upkilo and the external calendar
    /// </summary>
    Task SyncBookingsAsync(Guid staffId);

    /// <summary>
    /// Refreshes the access token if needed
    /// </summary>
    Task<string> GetValidAccessTokenAsync(CalendarSyncToken token);
}
