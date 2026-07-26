using Upkilo.Core.Entities;

namespace Upkilo.Core.Interfaces;

/// <summary>
/// Timezone conversion and management service
/// </summary>
public interface ITimezoneService
{
    DateTime ConvertToUserTimezone(DateTime utcTime, string timezoneId);
    DateTime ConvertToUtc(DateTime localTime, string timezoneId);
    string GetUserTimezone(Guid userId);
    IReadOnlyList<TimezoneInfo> GetAllTimezones();
    bool IsValidTimezone(string timezoneId);
    DateTimeOffset GetCurrentTime(string timezoneId);
    string GetBookingTimezone(Booking booking);
}

public record TimezoneInfo(string Id, string DisplayName, TimeSpan BaseUtcOffset);
