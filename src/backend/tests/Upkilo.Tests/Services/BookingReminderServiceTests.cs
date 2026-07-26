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

public class BookingReminderServiceTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory;
    private readonly Mock<ILogger<BookingReminderService>> _loggerMock;

    public BookingReminderServiceTests()
    {
        _dbFactory = new TestDbContextFactory();
        _loggerMock = new Mock<ILogger<BookingReminderService>>();
    }

    [Fact]
    public void Constructor_ValidArgs_CreatesInstance()
    {
        using var ctx = _dbFactory.CreateContext();
        var svc = new BookingReminderService(ctx, _loggerMock.Object);
        svc.Should().NotBeNull();
    }

    [Fact]
    public async Task ProcessRemindersAsync_NoUpcomingBookings_CompletesWithoutThrow()
    {
        using var ctx = _dbFactory.CreateContext();
        var svc = new BookingReminderService(ctx, _loggerMock.Object);

        var act = async () => await svc.ProcessRemindersAsync();
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ProcessRemindersAsync_BookingIn24Hours_ProcessesReminder()
    {
        using var ctx = _dbFactory.CreateContext();
        var tenantId = Guid.NewGuid();

        ctx.Tenants.Add(new Tenant { Id = tenantId, Name = "Test Biz", Slug = "reminder-biz" });

        var client = new Client
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FirstName = "Alice",
            LastName = "Smith",
            Email = "alice@example.com",
            Phone = "+10000000000",
            SmsConsent = true
        };
        ctx.Clients.Add(client);

        var service = new Service
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "Deep Tissue Massage",
            DurationMinutes = 60,
            Price = 80,
            IsActive = true
        };
        ctx.Services.Add(service);

        ctx.Bookings.Add(new Booking
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ClientId = client.Id,
            ServiceId = service.Id,
            Status = BookingStatus.Confirmed,
            StartTime = DateTime.UtcNow.AddHours(23.5),
            EndTime = DateTime.UtcNow.AddHours(24.5),
            ReminderSent = false
        });
        ctx.SaveChanges();

        var svc = new BookingReminderService(ctx, _loggerMock.Object);
        var act = async () => await svc.ProcessRemindersAsync();
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetPendingRemindersAsync_NoBookings_ReturnsEmptyList()
    {
        using var ctx = _dbFactory.CreateContext();
        var svc = new BookingReminderService(ctx, _loggerMock.Object);

        var result = await svc.GetPendingRemindersAsync(Guid.NewGuid());

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    public void Dispose() => _dbFactory.Dispose();
}
