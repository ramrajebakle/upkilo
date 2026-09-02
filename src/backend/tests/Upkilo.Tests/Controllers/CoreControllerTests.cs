using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Upkilo.API.Controllers;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Tests.Helpers;
using MockFactory = Upkilo.Tests.Helpers.MockFactory;
using MediatR;
using Microsoft.Extensions.Caching.Memory;

namespace Upkilo.Tests.Controllers;

/// <summary>
/// Tests for BookingsController — CRUD, status transitions, reschedule, walk-in, group bookings.
/// </summary>
public class BookingsControllerTests : ControllerTestBase
{
    private readonly BookingsController _sut;
    private readonly Mock<IBookingService> _bookingService;
    private readonly Mock<ISchedulingService> _schedulingService;
    private readonly Mock<ISubscriptionService> _subscriptionService;

    public BookingsControllerTests()
    {
        _bookingService = new Mock<IBookingService>();
        _schedulingService = MockFactory.CreateSchedulingService();
        _subscriptionService = MockFactory.CreateSubscriptionService();
        var logger = MockFactory.CreateLogger<BookingsController>();
        var eventService = MockFactory.CreateEventService();
        var mediator = new Mock<IMediator>();

        _sut = new BookingsController(logger.Object, eventService.Object, Context, TenantProvider.Object,
            _schedulingService.Object, _bookingService.Object, mediator.Object);
        WithAuth(_sut);
    }

    [Fact]
    public async Task GetBookings_ReturnsOkWithList()
    {
        // Seed bookings
        var service = TestFixtures.CreateService(TenantId);
        Context.Services.Add(service);
        var booking = TestFixtures.CreateBooking(TenantId, service.Id);
        Context.Bookings.Add(booking);
        await Context.SaveChangesAsync();

        var result = await _sut.GetBookings(1, 20, null, null, null);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetBooking_ValidId_ReturnsOk()
    {
        var service = TestFixtures.CreateService(TenantId);
        Context.Services.Add(service);
        var booking = TestFixtures.CreateBooking(TenantId, service.Id);
        Context.Bookings.Add(booking);
        await Context.SaveChangesAsync();

        var result = await _sut.GetBooking(booking.Id);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetBooking_InvalidId_ReturnsNotFound()
    {
        var result = await _sut.GetBooking(Guid.NewGuid());

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task CancelBooking_ValidBooking_ReturnsOk()
    {
        var service = TestFixtures.CreateService(TenantId);
        Context.Services.Add(service);
        var booking = TestFixtures.CreateBooking(TenantId, service.Id, status: BookingStatus.Confirmed);
        Context.Bookings.Add(booking);
        await Context.SaveChangesAsync();

        _bookingService.Setup(b => b.UpdateStatusAsync(
                TenantId, booking.Id, BookingStatus.Cancelled, It.IsAny<string>(), It.IsAny<byte[]?>()))
            .ReturnsAsync(booking);

        var result = await _sut.UpdateBooking(booking.Id, new UpdateBookingRequest(null, null, null, BookingStatus.Cancelled, "Client requested", null));

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetBookings_FilterByDate_ReturnsFilteredResults()
    {
        var service = TestFixtures.CreateService(TenantId);
        Context.Services.Add(service);
        var futureBooking = TestFixtures.CreateBooking(TenantId, service.Id);
        Context.Bookings.Add(futureBooking);
        await Context.SaveChangesAsync();

        var result = await _sut.GetBookings(1, 20, null, DateTime.UtcNow, DateTime.UtcNow.AddDays(7));

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetBookings_FilterByStatus_ReturnsFilteredResults()
    {
        var service = TestFixtures.CreateService(TenantId);
        Context.Services.Add(service);
        var booking = TestFixtures.CreateBooking(TenantId, service.Id, status: BookingStatus.Confirmed);
        Context.Bookings.Add(booking);
        await Context.SaveChangesAsync();

        var result = await _sut.GetBookings(1, 20, "Confirmed", null, null);

        result.Should().BeOfType<OkObjectResult>();
    }
}

/// <summary>
/// Tests for ClientsController — CRUD, search, merge, tags.
/// </summary>
public class ClientsControllerTests : ControllerTestBase
{
    private readonly ClientsController _sut;

    public ClientsControllerTests()
    {
        var logger = MockFactory.CreateLogger<ClientsController>();
        var eventService = MockFactory.CreateEventService();
        var loyaltyService = new Mock<ILoyaltyService>();
        var csvExportService = new Mock<ICsvExportService>();
        _sut = new ClientsController(logger.Object, eventService.Object, Context, TenantProvider.Object, loyaltyService.Object, csvExportService.Object, Upkilo.Tests.Helpers.MockFactory.CreateEntitlementService(Context));
        WithAuth(_sut);
    }

    [Fact]
    public async Task GetClients_ReturnsOk()
    {
        Context.Clients.Add(TestFixtures.CreateClient(TenantId));
        await Context.SaveChangesAsync();

        var result = await _sut.GetClients(1, 20, null);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetClient_ValidId_ReturnsOk()
    {
        var client = TestFixtures.CreateClient(TenantId);
        Context.Clients.Add(client);
        await Context.SaveChangesAsync();

        var result = await _sut.GetClient(client.Id);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetClient_InvalidId_ReturnsNotFound()
    {
        var result = await _sut.GetClient(Guid.NewGuid());

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task CreateClient_ValidData_ReturnsCreated()
    {
        var request = new CreateClientRequest(
            "Jane",
            "Doe",
            "jane@example.com",
            "+1234567890",
            null,
            false,
            false
        );

        var result = await _sut.CreateClient(request);

        result.Should().BeOfType<CreatedAtActionResult>();
        Context.Clients.Should().Contain(c => c.Email == "jane@example.com");
    }

    [Fact]
    public async Task DeleteClient_ValidId_ReturnsOk()
    {
        var client = TestFixtures.CreateClient(TenantId);
        Context.Clients.Add(client);
        await Context.SaveChangesAsync();

        var result = await _sut.DeleteClient(client.Id);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteClient_InvalidId_ReturnsNotFound()
    {
        var result = await _sut.DeleteClient(Guid.NewGuid());

        result.Should().BeOfType<NotFoundResult>();
    }

    // ControllerTestBase runs on SQLite, but client search uses EF.Functions.ILike, which only
    // the Npgsql provider can translate — so this fails with "could not be translated" regardless
    // of whether the controller is correct. Re-enable by moving it to the Testcontainers-backed
    // Postgres suite (see BookingIntegrationTests) rather than by weakening the production query.
    [Fact(Skip = "Requires Postgres: EF.Functions.ILike is not translatable on SQLite")]
    public async Task SearchClients_ByName_ReturnsMatches()
    {
        var client = TestFixtures.CreateClient(TenantId, email: "unique@test.com");
        Context.Clients.Add(client);
        await Context.SaveChangesAsync();

        var result = await _sut.GetClients(1, 20, client.FirstName);

        result.Should().BeOfType<OkObjectResult>();
    }
}

/// <summary>
/// Tests for StaffController — CRUD, availability, permissions.
/// </summary>
public class StaffControllerTests : ControllerTestBase
{
    private readonly StaffController _sut;

    public StaffControllerTests()
    {
        var logger = MockFactory.CreateLogger<StaffController>();
        var schedulingService = MockFactory.CreateSchedulingService();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var eventService = MockFactory.CreateEventService();
        _sut = new StaffController(logger.Object, Context, schedulingService.Object, TenantProvider.Object, cache, eventService.Object, Upkilo.Tests.Helpers.MockFactory.CreateEntitlementService(Context));
        WithAuth(_sut);
    }

    /// <summary>
    /// POST /api/v1/staff had no caller until the /staff/new page existed, so nothing had
    /// ever exercised it — and it returns 500 in production. This drives the real controller
    /// path to find out where.
    /// </summary>
    [Fact]
    public async Task CreateStaff_MinimalPayload_DoesNotThrow()
    {
        var request = new CreateStaffRequest(
            FirstName: "Ada",
            LastName: "Lovelace",
            Email: "ada@example.test",
            Phone: null,
            Role: "Stylist",
            Color: null,
            ServiceIds: null);

        var act = async () => await _sut.CreateStaff(request);

        await act.Should().NotThrowAsync();
        Context.Staff.Should().Contain(x => x.Email == "ada@example.test");
    }

    [Fact]
    public async Task GetStaff_ReturnsOk()
    {
        Context.Staff.Add(TestFixtures.CreateStaff(TenantId));
        await Context.SaveChangesAsync();

        var result = await _sut.GetStaff();

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetStaffMember_ValidId_ReturnsOk()
    {
        var staff = TestFixtures.CreateStaff(TenantId);
        Context.Staff.Add(staff);
        await Context.SaveChangesAsync();

        var result = await _sut.GetStaffMember(staff.Id);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetStaffMember_InvalidId_ReturnsNotFound()
    {
        var result = await _sut.GetStaffMember(Guid.NewGuid());

        result.Should().BeOfType<NotFoundResult>();
    }
}

/// <summary>
/// Tests for ServicesController — CRUD for bookable services.
/// </summary>
public class ServicesControllerTests : ControllerTestBase
{
    private readonly ServicesController _sut;

    public ServicesControllerTests()
    {
        var logger = MockFactory.CreateLogger<ServicesController>();
        var aiService = new Mock<IAIService>();
        var cacheMock = new Mock<ICacheService>();
        cacheMock.Setup(c => c.GetOrSetAsync<List<object>>(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Func<Task<List<object>>>>(), It.IsAny<TimeSpan?>()))
            // ICacheService.GetOrSetAsync returns Task<T?>, so the lambda must yield
            // Task<List<object>?>. Returning factory() directly gives Task<List<object>>,
            // a different generic instantiation, which raised CS8619.
            .Returns<Guid, string, Func<Task<List<object>>>, TimeSpan?>(
                async (_, _, factory, _) => (List<object>?)await factory());
        // ServicesController no longer takes IAIService: its one AI action receives it via
        // [FromServices] so plain CRUD requests never construct the AI stack.
        _sut = new ServicesController(logger.Object, Context, TenantProvider.Object, cacheMock.Object);
        WithAuth(_sut);
    }

    [Fact]
    public async Task GetServices_ReturnsOk()
    {
        Context.Services.Add(TestFixtures.CreateService(TenantId));
        await Context.SaveChangesAsync();

        var result = await _sut.GetServices();

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetService_ValidId_ReturnsOk()
    {
        var svc = TestFixtures.CreateService(TenantId);
        Context.Services.Add(svc);
        await Context.SaveChangesAsync();

        var result = await _sut.GetService(svc.Id);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetService_InvalidId_ReturnsNotFound()
    {
        var result = await _sut.GetService(Guid.NewGuid());

        result.Should().BeOfType<NotFoundResult>();
    }
}
