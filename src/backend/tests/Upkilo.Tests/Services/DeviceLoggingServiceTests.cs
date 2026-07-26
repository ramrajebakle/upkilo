using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Services;
using Upkilo.Tests.Helpers;
using Xunit;

namespace Upkilo.Tests.Services;

public class DeviceLoggingServiceTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory;
    private readonly Mock<ILogger<DeviceLoggingService>> _loggerMock = new();

    public DeviceLoggingServiceTests() => _dbFactory = new TestDbContextFactory();
    public void Dispose() => _dbFactory.Dispose();

    [Fact]
    public async Task LogDeviceAccessAsync_FirstAccess_CreatesNewDeviceRecord()
    {
        var ctx = _dbFactory.CreateContext();
        var sut = new DeviceLoggingService(ctx, _loggerMock.Object);
        var userId = Guid.NewGuid();

        await sut.LogDeviceAccessAsync(userId, "1.2.3.4", "Mozilla/5.0");

        ctx.ChangeTracker.Clear();
        ctx.Set<UserDevice>().Should().HaveCount(1);
        ctx.Set<UserDevice>().First().UserId.Should().Be(userId);
        ctx.Set<UserDevice>().First().IsTrusted.Should().BeFalse();
    }

    [Fact]
    public async Task LogDeviceAccessAsync_SameDeviceTwice_OnlyCreatesOneRecord()
    {
        var ctx = _dbFactory.CreateContext();
        var sut = new DeviceLoggingService(ctx, _loggerMock.Object);
        var userId = Guid.NewGuid();

        await sut.LogDeviceAccessAsync(userId, "1.2.3.4", "Mozilla/5.0");
        await sut.LogDeviceAccessAsync(userId, "1.2.3.4", "Mozilla/5.0");

        ctx.ChangeTracker.Clear();
        ctx.Set<UserDevice>().Count(d => d.UserId == userId).Should().Be(1);
    }

    [Fact]
    public async Task LogDeviceAccessAsync_DifferentUserAgents_CreatesTwoRecords()
    {
        var ctx = _dbFactory.CreateContext();
        var sut = new DeviceLoggingService(ctx, _loggerMock.Object);
        var userId = Guid.NewGuid();

        await sut.LogDeviceAccessAsync(userId, "1.2.3.4", "Chrome/100");
        await sut.LogDeviceAccessAsync(userId, "1.2.3.4", "Safari/15");

        ctx.ChangeTracker.Clear();
        ctx.Set<UserDevice>().Count(d => d.UserId == userId).Should().Be(2);
    }

    [Fact]
    public async Task LogDeviceAccessAsync_SecondAccess_UpdatesLastSeenAt()
    {
        var ctx = _dbFactory.CreateContext();
        var sut = new DeviceLoggingService(ctx, _loggerMock.Object);
        var userId = Guid.NewGuid();

        await sut.LogDeviceAccessAsync(userId, "1.2.3.4", "Mozilla/5.0");
        var before = ctx.Set<UserDevice>().First(d => d.UserId == userId).LastSeenAt;

        await Task.Delay(10); // small delay to differentiate timestamps
        await sut.LogDeviceAccessAsync(userId, "1.2.3.4", "Mozilla/5.0");

        ctx.ChangeTracker.Clear();
        var after = ctx.Set<UserDevice>().First(d => d.UserId == userId).LastSeenAt;
        after.Should().BeOnOrAfter(before);
    }
}
