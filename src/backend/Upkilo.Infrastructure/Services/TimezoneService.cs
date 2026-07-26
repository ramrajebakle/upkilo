using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

public class TimezoneService : ITimezoneService
{
    private readonly AppDbContext _context;

    public TimezoneService(AppDbContext context)
    {
        _context = context;
    }

    public DateTime ConvertToUserTimezone(DateTime utcTime, string timezoneId)
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcTime, DateTimeKind.Utc), tz);
    }

    public DateTime ConvertToUtc(DateTime localTime, string timezoneId)
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
        return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localTime, DateTimeKind.Unspecified), tz);
    }

    public string GetUserTimezone(Guid userId)
    {
        var user = _context.Users.FirstOrDefault(u => u.Id == userId);
        if (user?.Preferences != null && user.Preferences.TryGetValue("timezone", out var tz))
        {
            return tz?.ToString() ?? "UTC";
        }
        return "UTC";
    }

    public IReadOnlyList<TimezoneInfo> GetAllTimezones()
    {
        return TimeZoneInfo.GetSystemTimeZones()
            .Select(tz => new TimezoneInfo(tz.Id, tz.DisplayName, tz.BaseUtcOffset))
            .ToList()
            .AsReadOnly();
    }

    public bool IsValidTimezone(string timezoneId)
    {
        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
    }

    public DateTimeOffset GetCurrentTime(string timezoneId)
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
        return TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, tz);
    }

    public string GetBookingTimezone(Booking booking)
    {
        if (booking == null) return "UTC";

        // 1. Check if explicitly set on booking
        if (!string.IsNullOrEmpty(booking.Timezone)) return booking.Timezone;

        // 2. Check Staff Timezone
        if (booking.Staff != null && !string.IsNullOrEmpty(booking.Staff.Timezone) && booking.Staff.Timezone != "UTC")
            return booking.Staff.Timezone;

        // 3. Check Location Timezone (via Service context if needed, but Booking has LocationId)
        // Note: For now, we assume if Staff has a timezone, that takes precedence.
        
        // 4. Check Tenant Timezone
        if (booking.Tenant != null && !string.IsNullOrEmpty(booking.Tenant.Timezone))
            return booking.Tenant.Timezone;

        return "UTC";
    }
}
