using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Services;
using Upkilo.Tests.Helpers;
using Xunit;

namespace Upkilo.Tests.Services;

public class DataErasureServiceTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory;
    private readonly Mock<IPiiScrubberService> _piiScrubberMock = new();
    private readonly Mock<ILogger<DataErasureService>> _loggerMock = new();

    public DataErasureServiceTests() => _dbFactory = new TestDbContextFactory();
    public void Dispose() => _dbFactory.Dispose();

    [Fact]
    public async Task EraseUserAsync_WhenUserExists_DeletesUser()
    {
        var ctx = _dbFactory.CreateContext();
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "T", Slug = "t" };
        ctx.Tenants.Add(tenant);
        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Email = "target@test.com",
            FirstName = "John",
            LastName = "Doe"
        };
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();

        var sut = new DataErasureService(ctx, _piiScrubberMock.Object, _loggerMock.Object);
        await sut.EraseUserAsync(user.Id);

        ctx.ChangeTracker.Clear();
        ctx.Users.Find(user.Id).Should().BeNull(); // User deleted
    }

    [Fact]
    public async Task EraseUserAsync_WhenUserNotFound_DoesNotThrow()
    {
        var ctx = _dbFactory.CreateContext();
        var sut = new DataErasureService(ctx, _piiScrubberMock.Object, _loggerMock.Object);

        var act = async () => await sut.EraseUserAsync(Guid.NewGuid());

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task EraseUserAsync_AnonymizesLinkedBookings()
    {
        var ctx = _dbFactory.CreateContext();
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "T", Slug = "t" };
        ctx.Tenants.Add(tenant);
        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Email = "target@test.com",
            FirstName = "Jane"
        };
        ctx.Users.Add(user);
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            CustomerEmail = "target@test.com",
            CustomerName = "Jane Doe",
            StartTime = DateTime.UtcNow.AddDays(-1),
            Status = BookingStatus.Completed
        };
        ctx.Bookings.Add(booking);
        await ctx.SaveChangesAsync();

        var sut = new DataErasureService(ctx, _piiScrubberMock.Object, _loggerMock.Object);
        await sut.EraseUserAsync(user.Id);

        ctx.ChangeTracker.Clear();
        var erasedBooking = ctx.Bookings.Find(booking.Id);
        erasedBooking!.CustomerName.Should().Be("ANONYMIZED");
        erasedBooking.CustomerEmail.Should().Be("anonymized@upkilo.com");
    }
}
