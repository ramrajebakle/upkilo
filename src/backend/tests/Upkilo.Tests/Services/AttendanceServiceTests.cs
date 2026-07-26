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

public class AttendanceServiceTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory;
    private readonly Mock<ILogger<AttendanceService>> _loggerMock = new();

    public AttendanceServiceTests() => _dbFactory = new TestDbContextFactory();
    public void Dispose() => _dbFactory.Dispose();

    private (AttendanceService sut, Upkilo.Infrastructure.Data.AppDbContext ctx) CreateSut()
    {
        var ctx = _dbFactory.CreateContext();
        return (new AttendanceService(ctx, _loggerMock.Object), ctx);
    }

    private async Task<(Guid tenantId, Guid staffId)> SeedTenantAndStaff(Upkilo.Infrastructure.Data.AppDbContext ctx)
    {
        var tenantId = Guid.NewGuid();
        var staffId = Guid.NewGuid();
        ctx.Tenants.Add(new Tenant { Id = tenantId, Name = "T", Slug = "t" });
        ctx.Set<StaffMember>().Add(new StaffMember { Id = staffId, TenantId = tenantId, FirstName = "Alice", LastName = "Smith" });
        await ctx.SaveChangesAsync();
        return (tenantId, staffId);
    }

    [Fact]
    public async Task ClockInAsync_WhenNoPriorSession_CreatesClockInRecord()
    {
        var (sut, ctx) = CreateSut();
        var (tenantId, staffId) = await SeedTenantAndStaff(ctx);

        var clockIn = await sut.ClockInAsync(tenantId, staffId, ipAddress: "127.0.0.1");

        clockIn.Should().NotBeNull();
        clockIn.StaffId.Should().Be(staffId);
        clockIn.ClockOutTime.Should().BeNull();
        clockIn.IpAddress.Should().Be("127.0.0.1");
    }

    [Fact]
    public async Task ClockInAsync_WhenAlreadyClockedIn_ThrowsException()
    {
        var (sut, ctx) = CreateSut();
        var (tenantId, staffId) = await SeedTenantAndStaff(ctx);

        await sut.ClockInAsync(tenantId, staffId);
        var act = () => sut.ClockInAsync(tenantId, staffId);

        await act.Should().ThrowAsync<Exception>().WithMessage("*already clocked in*");
    }

    [Fact]
    public async Task ClockOutAsync_WhenClockedIn_RecordsClockOutTime()
    {
        var (sut, ctx) = CreateSut();
        var (tenantId, staffId) = await SeedTenantAndStaff(ctx);

        await sut.ClockInAsync(tenantId, staffId);
        var clockOut = await sut.ClockOutAsync(staffId);

        clockOut.ClockOutTime.Should().NotBeNull();
        clockOut.ClockOutTime!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task ClockOutAsync_WhenNotClockedIn_ThrowsException()
    {
        var (sut, ctx) = CreateSut();
        var (_, staffId) = await SeedTenantAndStaff(ctx);

        var act = () => sut.ClockOutAsync(staffId);

        await act.Should().ThrowAsync<Exception>().WithMessage("*No active clock-in*");
    }

    [Fact]
    public async Task GetStaffTimesheetAsync_FiltersToDateRange()
    {
        var (sut, ctx) = CreateSut();
        var (tenantId, staffId) = await SeedTenantAndStaff(ctx);

        await sut.ClockInAsync(tenantId, staffId);
        await sut.ClockOutAsync(staffId);

        var start = DateTime.UtcNow.AddHours(-1);
        var end = DateTime.UtcNow.AddHours(1);
        var sheet = await sut.GetStaffTimesheetAsync(staffId, start, end);

        sheet.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetAttendanceStatsAsync_CalculatesHoursWorked()
    {
        var (sut, ctx) = CreateSut();
        var (tenantId, staffId) = await SeedTenantAndStaff(ctx);

        await sut.ClockInAsync(tenantId, staffId);
        await sut.ClockOutAsync(staffId);

        var stats = await sut.GetAttendanceStatsAsync(tenantId,
            DateTime.UtcNow.AddHours(-1), DateTime.UtcNow.AddHours(1));

        stats.TotalClockIns.Should().Be(1);
        stats.TotalHoursWorked.Should().BeGreaterThanOrEqualTo(0);
    }
}
