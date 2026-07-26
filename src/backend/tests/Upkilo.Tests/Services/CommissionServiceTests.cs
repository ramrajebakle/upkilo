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

public class CommissionServiceTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory;
    private readonly Mock<ILogger<CommissionService>> _loggerMock;

    public CommissionServiceTests()
    {
        _dbFactory = new TestDbContextFactory();
        _loggerMock = new Mock<ILogger<CommissionService>>();
    }

    [Fact]
    public void Constructor_ValidArgs_CreatesInstance()
    {
        using var ctx = _dbFactory.CreateContext();
        var svc = new CommissionService(ctx, _loggerMock.Object);
        svc.Should().NotBeNull();
    }

    [Fact]
    public async Task CalculateCommissionAsync_BookingNotFound_ThrowsException()
    {
        using var ctx = _dbFactory.CreateContext();
        var svc = new CommissionService(ctx, _loggerMock.Object);

        var act = async () => await svc.CalculateCommissionAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 100m);
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task CalculateCommissionAsync_ValidBookingNoRule_UsesStaffBaseRate()
    {
        using var ctx = _dbFactory.CreateContext();
        var tenantId = Guid.NewGuid();
        var staffId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();

        ctx.Tenants.Add(new Tenant { Id = tenantId, Name = "Test", Slug = "test-commission" });

        ctx.StaffMembers.Add(new StaffMember
        {
            Id = staffId,
            TenantId = tenantId,
            FirstName = "Bob",
            LastName = "Staff",
            Email = "bob@example.com",
            BaseCommissionRate = 10m,
            CommissionType = CommissionType.Percentage,
            IsActive = true
        });

        ctx.Services.Add(new Service
        {
            Id = serviceId,
            TenantId = tenantId,
            Name = "Haircut",
            DurationMinutes = 30,
            Price = 50,
            IsActive = true
        });

        ctx.Bookings.Add(new Booking
        {
            Id = bookingId,
            TenantId = tenantId,
            StaffId = staffId,
            ServiceId = serviceId,
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddMinutes(30),
            Status = BookingStatus.Confirmed
        });
        ctx.SaveChanges();

        var svc = new CommissionService(ctx, _loggerMock.Object);
        var result = await svc.CalculateCommissionAsync(tenantId, staffId, bookingId, 50m);

        result.Should().NotBeNull();
        result.TotalEarned.Should().Be(5m); // 10% of 50
        result.StaffId.Should().Be(staffId);
    }

    [Fact]
    public async Task GetStaffEarningsAsync_NoCommissions_ReturnsEmptyCollection()
    {
        using var ctx = _dbFactory.CreateContext();
        var svc = new CommissionService(ctx, _loggerMock.Object);

        var result = await svc.GetStaffEarningsAsync(Guid.NewGuid());

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    public void Dispose() => _dbFactory.Dispose();
}
