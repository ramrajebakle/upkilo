using System;
using FluentAssertions;
using Upkilo.Infrastructure.Services;
using Upkilo.Tests.Helpers;
using Xunit;

namespace Upkilo.Tests.Services;

public class TimezoneServiceTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory;
    public TimezoneServiceTests() => _dbFactory = new TestDbContextFactory();
    public void Dispose() => _dbFactory.Dispose();

    private TimezoneService CreateSut() => new TimezoneService(_dbFactory.CreateContext());

    [Fact]
    public void IsValidTimezone_KnownTimezone_ReturnsTrue()
    {
        var sut = CreateSut();
        // Use a cross-platform timezone ID
        var result = sut.IsValidTimezone("UTC");
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidTimezone_UnknownTimezone_ReturnsFalse()
    {
        var sut = CreateSut();
        var result = sut.IsValidTimezone("Mars/Olympus_Mons");
        result.Should().BeFalse();
    }

    [Fact]
    public void GetAllTimezones_ReturnsNonEmptyList()
    {
        var sut = CreateSut();
        var timezones = sut.GetAllTimezones();
        timezones.Should().NotBeEmpty();
    }

    [Fact]
    public void ConvertToUserTimezone_UtcInput_ReturnsExpectedLocalTime()
    {
        var sut = CreateSut();
        var utcTime = new DateTime(2024, 6, 1, 12, 0, 0, DateTimeKind.Utc);

        // UTC stays the same
        var result = sut.ConvertToUserTimezone(utcTime, "UTC");

        result.Should().Be(new DateTime(2024, 6, 1, 12, 0, 0));
    }

    [Fact]
    public void GetCurrentTime_UTC_ReturnsCurrentTime()
    {
        var sut = CreateSut();
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);

        var result = sut.GetCurrentTime("UTC");

        result.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void GetUserTimezone_UserWithNoPreferences_ReturnsUtc()
    {
        var sut = CreateSut();

        var tz = sut.GetUserTimezone(Guid.NewGuid());

        tz.Should().Be("UTC");
    }
}
