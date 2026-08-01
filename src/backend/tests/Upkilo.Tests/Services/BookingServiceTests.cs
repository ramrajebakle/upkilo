using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Services;
using Upkilo.Tests.Helpers;
using MediatR;

namespace Upkilo.Tests.Services;

/// <summary>
/// Unit tests for BookingService — covers the core booking lifecycle:
/// create, status transitions, reschedule, availability checks, and edge cases.
/// Uses SQLite in-memory DB via TestDbContextFactory.
/// </summary>
public class BookingServiceTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory;
    private readonly BookingService _sut;
    private readonly Mock<ISchedulingService> _schedulingService;
    private readonly Mock<IEventService> _eventService;
    private readonly Mock<IMediator> _mediator;

    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _staffId = Guid.NewGuid();
    private readonly Guid _clientId = Guid.NewGuid();
    private readonly Guid _serviceId = Guid.NewGuid();

    public BookingServiceTests()
    {
        _dbFactory = new TestDbContextFactory();
        var context = _dbFactory.CreateContext();

        _schedulingService = new Mock<ISchedulingService>();
        _eventService = new Mock<IEventService>();
        _mediator = new Mock<IMediator>();
        var logger = new Mock<ILogger<BookingService>>();

        // Default: slots are available, concurrency OK
        _schedulingService.Setup(s => s.CheckConcurrencyLimitAsync(It.IsAny<Guid>())).ReturnsAsync(true);
        _schedulingService.Setup(s => s.IsSlotAvailableAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid?>(),
            It.IsAny<DateTime>(), It.IsAny<int>())).ReturnsAsync(true);
        _schedulingService.Setup(s => s.UpdateAvailabilityCacheAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateOnly>())).Returns(Task.CompletedTask);
        _schedulingService.Setup(s => s.InvalidateStaffCacheAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateOnly>())).Returns(Task.CompletedTask);
        _eventService.Setup(e => e.PublishAsync(
            It.IsAny<string>(), It.IsAny<object>(), It.IsAny<Guid>())).Returns(Task.CompletedTask);

        // Seed a tenant first to satisfy foreign key constraint
        context.Tenants.Add(new Tenant
        {
            Id = _tenantId,
            Name = "Test Tenant",
            Slug = "test-tenant",
            Status = TenantStatus.Active,
            CreatedAt = DateTime.UtcNow
        });

        // Seed a client to satisfy foreign key constraint
        context.Clients.Add(new Client
        {
            Id = _clientId,
            TenantId = _tenantId,
            FirstName = "Test",
            LastName = "Client",
            Email = "client@example.com",
            CreatedAt = DateTime.UtcNow
        });

        // Seed a staff member to satisfy foreign key constraint
        context.StaffMembers.Add(new StaffMember
        {
            Id = _staffId,
            TenantId = _tenantId,
            FirstName = "Test",
            LastName = "Staff",
            Email = "staff@example.com",
            CreatedAt = DateTime.UtcNow
        });

        // Seed a service
        context.Services.Add(new Service
        {
            Id = _serviceId,
            TenantId = _tenantId,
            Name = "Haircut",
            DurationMinutes = 30,
            Price = 50.00m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        context.SaveChanges();

        _sut = new BookingService(context, logger.Object, _schedulingService.Object, _eventService.Object, _mediator.Object);
    }

    public void Dispose() => _dbFactory.Dispose();

    // ---- CreateBookingAsync ----

    [Fact]
    public async Task CreateBookingAsync_ValidModel_CreatesBookingAndReturnsIt()
    {
        var model = new CreateBookingModel(
            _clientId, _serviceId, _staffId,
            DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddMinutes(30),
            "Test booking"
        );

        var result = await _sut.CreateBookingAsync(_tenantId, model);

        result.Should().NotBeNull();
        result.TenantId.Should().Be(_tenantId);
        result.ServiceId.Should().Be(_serviceId);
        result.Status.Should().Be(BookingStatus.Confirmed);
        result.Price.Should().Be(50.00m);
    }

    [Fact]
    public async Task CreateBookingAsync_InvalidService_Throws()
    {
        var model = new CreateBookingModel(
            null, Guid.NewGuid(), _staffId,
            DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddMinutes(30),
            null
        );

        var act = () => _sut.CreateBookingAsync(_tenantId, model);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Service not found*");
    }

    [Fact]
    public async Task CreateBookingAsync_ConcurrencyLimitReached_Throws()
    {
        _schedulingService.Setup(s => s.CheckConcurrencyLimitAsync(_tenantId)).ReturnsAsync(false);

        var model = new CreateBookingModel(
            null, _serviceId, _staffId,
            DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddMinutes(30),
            null
        );

        var act = () => _sut.CreateBookingAsync(_tenantId, model);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Concurrency limit*");
    }

    [Fact]
    public async Task CreateBookingAsync_SlotUnavailable_Throws()
    {
        _schedulingService.Setup(s => s.IsSlotAvailableAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid?>(),
            It.IsAny<DateTime>(), It.IsAny<int>())).ReturnsAsync(false);

        var model = new CreateBookingModel(
            null, _serviceId, _staffId,
            DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddMinutes(30),
            null
        );

        var act = () => _sut.CreateBookingAsync(_tenantId, model);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*no longer available*");
    }

    [Fact]
    public async Task CreateBookingAsync_WalkIn_SetsCheckedInTimestamp()
    {
        var model = new CreateBookingModel(
            _clientId, _serviceId, _staffId,
            DateTime.UtcNow, DateTime.UtcNow.AddMinutes(30),
            null, 1, true
        );

        var result = await _sut.CreateBookingAsync(_tenantId, model);

        result.IsWalkIn.Should().BeTrue();
        result.CheckedInAt.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateBookingAsync_PublishesDomainEvents()
    {
        var model = new CreateBookingModel(
            _clientId, _serviceId, _staffId,
            DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddMinutes(30),
            null
        );

        await _sut.CreateBookingAsync(_tenantId, model);

        _eventService.Verify(e => e.PublishAsync("booking.created", It.IsAny<object>(), _tenantId), Times.Once);
        _mediator.Verify(m => m.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---- UpdateStatusAsync ----

    [Fact]
    public async Task UpdateStatusAsync_ConfirmedToCompleted_PublishesCompletedEvent()
    {
        // Seed booking
        var context = _dbFactory.CreateContext();
        var bookingId = Guid.NewGuid();
        context.Bookings.Add(new Booking
        {
            Id = bookingId,
            TenantId = _tenantId,
            ServiceId = _serviceId,
            StaffId = _staffId,
            StartTime = DateTime.UtcNow.AddDays(-1),
            EndTime = DateTime.UtcNow.AddDays(-1).AddMinutes(30),
            Status = BookingStatus.Confirmed,
            Price = 50m,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var result = await _sut.UpdateStatusAsync(_tenantId, bookingId, BookingStatus.Completed, rowVersion: new byte[] { 1 });

        result.Status.Should().Be(BookingStatus.Completed);
        _eventService.Verify(e => e.PublishAsync("booking.completed", It.IsAny<object>(), _tenantId), Times.Once);
    }

    [Fact]
    public async Task UpdateStatusAsync_SameStatus_NoOp()
    {
        var context = _dbFactory.CreateContext();
        var bookingId = Guid.NewGuid();
        context.Bookings.Add(new Booking
        {
            Id = bookingId,
            TenantId = _tenantId,
            ServiceId = _serviceId,
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddMinutes(30),
            Status = BookingStatus.Confirmed,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var result = await _sut.UpdateStatusAsync(_tenantId, bookingId, BookingStatus.Confirmed, rowVersion: new byte[] { 1 });

        result.Status.Should().Be(BookingStatus.Confirmed);
        _eventService.Verify(e => e.PublishAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task UpdateStatusAsync_NonexistentBooking_Throws()
    {
        var act = () => _sut.UpdateStatusAsync(_tenantId, Guid.NewGuid(), BookingStatus.Completed, rowVersion: new byte[] { 1 });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Booking not found*");
    }

    // ---- Availability ----

    [Fact]
    public async Task IsAvailableAsync_DelegatesToSchedulingService()
    {
        var result = await _sut.IsAvailableAsync(_tenantId, _serviceId, _staffId, DateTime.UtcNow.AddDays(1), 30);

        result.Should().BeTrue();
        _schedulingService.Verify(s => s.IsSlotAvailableAsync(_tenantId, _serviceId, _staffId, It.IsAny<DateTime>(), 30), Times.Once);
    }

    // ---- RescheduleBookingAsync ----

    [Fact]
    public async Task RescheduleBookingAsync_ValidReschedule_UpdatesBooking()
    {
        var context = _dbFactory.CreateContext();
        var bookingId = Guid.NewGuid();
        var booking = new Booking
        {
            Id = bookingId,
            TenantId = _tenantId,
            ClientId = _clientId,
            ServiceId = _serviceId,
            StaffId = _staffId,
            StartTime = DateTime.UtcNow.AddDays(2),
            EndTime = DateTime.UtcNow.AddDays(2).AddMinutes(30),
            Status = BookingStatus.Confirmed,
            CreatedAt = DateTime.UtcNow,
            RescheduleCount = 0
        };
        context.Bookings.Add(booking);
        await context.SaveChangesAsync();

        var newTime = DateTime.UtcNow.AddDays(3);

        var result = await _sut.RescheduleBookingAsync(_tenantId, bookingId, newTime, rowVersion: new byte[] { 1 }, bypassCodeCheck: true);

        result.Should().NotBeNull();
        result.StartTime.Should().Be(newTime);
        result.RescheduleCount.Should().Be(1);

        _eventService.Verify(e => e.PublishAsync("booking.rescheduled", It.IsAny<object>(), _tenantId), Times.Once);
    }

    [Fact]
    public async Task RescheduleBookingAsync_CancelledBooking_Throws()
    {
        var context = _dbFactory.CreateContext();
        var bookingId = Guid.NewGuid();
        var booking = new Booking
        {
            Id = bookingId,
            TenantId = _tenantId,
            ClientId = _clientId,
            ServiceId = _serviceId,
            StaffId = _staffId,
            StartTime = DateTime.UtcNow.AddDays(2),
            EndTime = DateTime.UtcNow.AddDays(2).AddMinutes(30),
            Status = BookingStatus.Cancelled,
            CreatedAt = DateTime.UtcNow
        };
        context.Bookings.Add(booking);
        await context.SaveChangesAsync();

        var act = () => _sut.RescheduleBookingAsync(_tenantId, bookingId, DateTime.UtcNow.AddDays(3), rowVersion: new byte[] { 1 }, bypassCodeCheck: true);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Cannot reschedule a cancelled or completed booking*");
    }

    // ---- CreateRecurringBookingAsync ----

    [Fact]
    public async Task CreateRecurringBookingAsync_ValidPattern_CreatesMultipleBookings()
    {
        var candidateDates = new List<DateTime> { DateTime.UtcNow.Date.AddDays(7), DateTime.UtcNow.Date.AddDays(14) };
        _schedulingService.Setup(s => s.GenerateRecurrenceDatesAsync(
            _tenantId, It.IsAny<string>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime?>(), It.IsAny<int?>(), It.IsAny<List<int>?>()))
            .ReturnsAsync(candidateDates);

        var model = new CreateRecurringBookingModel(
            _clientId, _serviceId, _staffId, DateTime.UtcNow.Date, "weekly", 1,
            new List<int> { (int)DayOfWeek.Monday }, DateTime.UtcNow.Date.AddDays(14), null,
            TimeSpan.FromHours(10), "Recurring notes", 1
        );

        var result = await _sut.CreateRecurringBookingAsync(_tenantId, model);

        result.Should().NotBeNull();
        result.SuccessCount.Should().Be(2);
        result.ConflictCount.Should().Be(0);

        var context = _dbFactory.CreateContext();
        context.Bookings.Where(b => b.RecurringPatternId == result.PatternId).Should().HaveCount(2);
    }
}
