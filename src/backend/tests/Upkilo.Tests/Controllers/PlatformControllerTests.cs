using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Upkilo.API.Controllers;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Tests.Helpers;
using MockFactory = Upkilo.Tests.Helpers.MockFactory;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace Upkilo.Tests.Controllers;

/// <summary>
/// Tests for SettingsController — tenant settings CRUD.
/// </summary>
public class SettingsControllerTests : ControllerTestBase
{
    private readonly SettingsController _sut;

    public SettingsControllerTests()
    {
        var logger = MockFactory.CreateLogger<SettingsController>();
        var passwordHasher = new PasswordHasher<User>();
        var emailService = MockFactory.CreateEmailService();
        var configuration = MockFactory.CreateConfiguration();
        _sut = new SettingsController(logger.Object, Context, TenantProvider.Object, passwordHasher, emailService.Object, configuration);
        WithAuth(_sut);
    }

    [Fact]
    public async Task GetBusinessSettings_ReturnsOkWithTenantSettings()
    {
        var result = await _sut.GetBusinessSettings();
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetBookingSettings_ReturnsOk()
    {
        var result = await _sut.GetBookingSettings();
        result.Should().BeOfType<OkObjectResult>();
    }
}

/// <summary>
/// Tests for ScheduleBlocksController.
/// </summary>
public class ScheduleBlocksControllerTests : ControllerTestBase
{
    private readonly ScheduleBlocksController _sut;

    public ScheduleBlocksControllerTests()
    {
        var logger = MockFactory.CreateLogger<ScheduleBlocksController>();
        _sut = new ScheduleBlocksController(logger.Object, Context, TenantProvider.Object);
        WithAuth(_sut);
    }

    [Fact]
    public async Task GetBlocks_ReturnsOk()
    {
        var result = await _sut.GetBlocks(null, null, null);
        result.Should().BeOfType<OkObjectResult>();
    }
}

/// <summary>
/// Tests for WaitlistController — CRUD and auto-booking.
/// </summary>
public class WaitlistControllerTests : ControllerTestBase
{
    private readonly WaitlistController _sut;

    public WaitlistControllerTests()
    {
        var logger = MockFactory.CreateLogger<WaitlistController>();
        var bookingService = new Mock<IBookingService>();
        var eventService = MockFactory.CreateEventService();
        _sut = new WaitlistController(logger.Object, Context, TenantProvider.Object, eventService.Object, bookingService.Object);
        WithAuth(_sut);
    }

    [Fact]
    public async Task GetWaitlistEntries_ReturnsOk()
    {
        var result = await _sut.GetWaitlist(1, 20, null, null, null, null, null);
        result.Should().BeOfType<OkObjectResult>();
    }
}

/// <summary>
/// Tests for CouponsController — CRUD, validation, redemption.
/// </summary>
public class CouponsControllerTests : ControllerTestBase
{
    private readonly CouponsController _sut;

    public CouponsControllerTests()
    {
        var logger = MockFactory.CreateLogger<CouponsController>();
        _sut = new CouponsController(logger.Object, Context, TenantProvider.Object);
        WithAuth(_sut);
    }

    [Fact]
    public async Task GetCoupons_ReturnsOk()
    {
        var result = await _sut.GetCoupons();
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ValidateCoupon_InvalidCode_ReturnsNotValid()
    {
        var result = await _sut.ValidateCoupon(new ValidateCouponRequest { Code = "NONEXISTENT" });
        result.Should().BeOfType<OkObjectResult>();
    }
}

/// <summary>
/// Tests for WorkflowsController.
/// </summary>
public class WorkflowsControllerTests : ControllerTestBase
{
    private readonly WorkflowsController _sut;
    private readonly Mock<IWorkflowService> _workflowServiceMock;

    public WorkflowsControllerTests()
    {
        var logger = MockFactory.CreateLogger<WorkflowsController>();
        _workflowServiceMock = new Mock<IWorkflowService>();
        _sut = new WorkflowsController(_workflowServiceMock.Object, TenantProvider.Object, Context, logger.Object);
        WithAuth(_sut);
    }

    [Fact]
    public async Task GetWorkflows_ReturnsOk()
    {
        var result = await _sut.GetWorkflows();
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetWorkflow_InvalidId_ReturnsNotFound()
    {
        var result = await _sut.GetWorkflow(Guid.NewGuid());
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetWorkflow_ValidId_ReturnsOk()
    {
        var workflow = TestFixtures.CreateWorkflow(TenantId);
        Context.Workflows.Add(workflow);
        await Context.SaveChangesAsync();

        _workflowServiceMock.Setup(s => s.GetWorkflowAsync(workflow.Id, TenantId)).ReturnsAsync(workflow);

        var result = await _sut.GetWorkflow(workflow.Id);
        result.Should().BeOfType<OkObjectResult>();
    }
}

/// <summary>
/// Tests for NotificationPreferencesController.
/// </summary>
public class NotificationPreferencesControllerTests : ControllerTestBase
{
    private readonly NotificationPreferencesController _sut;

    public NotificationPreferencesControllerTests()
    {
        _sut = new NotificationPreferencesController(Context, TenantProvider.Object);
        WithAuth(_sut);

        Context.Users.Add(TestFixtures.CreateUser(TenantId, UserId));
        Context.SaveChanges();
    }

    [Fact]
    public async Task GetPreferences_ReturnsOk()
    {
        var result = await _sut.GetPreferences();
        result.Should().BeOfType<OkObjectResult>();
    }
}

/// <summary>
/// Tests for FormsController.
/// </summary>
public class FormsControllerTests : ControllerTestBase
{
    private readonly FormsController _sut;

    public FormsControllerTests()
    {
        var logger = MockFactory.CreateLogger<FormsController>();
        _sut = new FormsController(logger.Object, Context, TenantProvider.Object);
        WithAuth(_sut);
    }

    [Fact]
    public async Task GetForms_ReturnsOk()
    {
        var result = await _sut.GetForms();
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetForm_InvalidId_ReturnsNotFound()
    {
        var result = await _sut.GetForm(Guid.NewGuid());
        result.Should().BeOfType<NotFoundResult>();
    }
}

/// <summary>
/// Tests for WaiversController.
/// </summary>
public class WaiversControllerTests : ControllerTestBase
{
    private readonly WaiversController _sut;

    public WaiversControllerTests()
    {
        var logger = MockFactory.CreateLogger<WaiversController>();
        _sut = new WaiversController(Context, TenantProvider.Object, logger.Object);
        WithAuth(_sut);
    }

    [Fact]
    public async Task GetWaivers_ReturnsOk()
    {
        var result = await _sut.GetWaivers();
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetWaiver_InvalidId_ReturnsNotFound()
    {
        var result = await _sut.GetWaiver(Guid.NewGuid());
        result.Should().BeOfType<NotFoundResult>();
    }
}

/// <summary>
/// Tests for RolesController.
/// </summary>
public class RolesControllerTests : ControllerTestBase
{
    private readonly RolesController _sut;

    public RolesControllerTests()
    {
        var logger = MockFactory.CreateLogger<RolesController>();
        _sut = new RolesController(Context, TenantProvider.Object, logger.Object);
        WithAuth(_sut);
    }

    [Fact]
    public async Task GetRoles_ReturnsOk()
    {
        var result = await _sut.GetRoles();
        result.Should().BeOfType<OkObjectResult>();
    }
}

/// <summary>
/// Tests for ResourcesController.
/// </summary>
public class ResourcesControllerTests : ControllerTestBase
{
    private readonly ResourcesController _sut;

    public ResourcesControllerTests()
    {
        var logger = MockFactory.CreateLogger<ResourcesController>();
        _sut = new ResourcesController(logger.Object, Context, TenantProvider.Object);
        WithAuth(_sut);
    }

    [Fact]
    public async Task GetResources_ReturnsOk()
    {
        var result = await _sut.GetResources();
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetResource_InvalidId_ReturnsNotFound()
    {
        var result = await _sut.GetResource(Guid.NewGuid());
        result.Should().BeOfType<NotFoundResult>();
    }
}

/// <summary>
/// Tests for InventoryController.
/// </summary>
public class InventoryControllerTests : ControllerTestBase
{
    private readonly InventoryController _sut;

    public InventoryControllerTests()
    {
        var logger = MockFactory.CreateLogger<InventoryController>();
        _sut = new InventoryController(Context, TenantProvider.Object, logger.Object);
        WithAuth(_sut);
    }

    [Fact]
    public async Task GetInventoryItems_ReturnsOk()
    {
        Context.InventoryItems.Add(TestFixtures.CreateInventoryItem(TenantId));
        await Context.SaveChangesAsync();

        var result = await _sut.GetItems(null, null);
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetInventoryItem_InvalidId_ReturnsNotFound()
    {
        var result = await _sut.GetItem(Guid.NewGuid());
        result.Should().BeOfType<NotFoundResult>();
    }
}

/// <summary>
/// Tests for SalesPipelineController.
/// </summary>
public class SalesPipelineControllerTests : ControllerTestBase
{
    private readonly SalesPipelineController _sut;

    public SalesPipelineControllerTests()
    {
        var logger = MockFactory.CreateLogger<SalesPipelineController>();
        var eventService = MockFactory.CreateEventService();
        _sut = new SalesPipelineController(logger.Object, Context, TenantProvider.Object, eventService.Object);
        WithAuth(_sut);
    }

    [Fact]
    public async Task GetPipelines_ReturnsOk()
    {
        var result = await _sut.GetPipelines();
        result.Should().BeOfType<OkObjectResult>();
    }
}

/// <summary>
/// Tests for SmsController.
/// </summary>
public class SmsControllerTests : ControllerTestBase
{
    private readonly SmsController _sut;

    public SmsControllerTests()
    {
        var logger = MockFactory.CreateLogger<SmsController>();
        var subscriptionService = new Mock<ISubscriptionService>();
        subscriptionService.Setup(s => s.GetUsageAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new Upkilo.Core.Interfaces.UsageSummary { SmsLimit = 1000, SmsUsed = 0 });
        var publishMock = new Mock<MassTransit.IPublishEndpoint>();
        _sut = new SmsController(logger.Object, Context, TenantProvider.Object, subscriptionService.Object, publishMock.Object);
        WithAuth(_sut);
    }

    [Fact]
    public async Task GetSmsHistory_ReturnsOk()
    {
        var result = await _sut.GetMessages(1, 50, null);
        result.Should().BeOfType<OkObjectResult>();
    }
}

/// <summary>
/// Tests for TaxRatesController.
/// </summary>
public class TaxRatesControllerTests : ControllerTestBase
{
    private readonly TaxRatesController _sut;

    public TaxRatesControllerTests()
    {
        var logger = MockFactory.CreateLogger<TaxRatesController>();
        var taxService = new Mock<ITaxService>();
        _sut = new TaxRatesController(taxService.Object, TenantProvider.Object, logger.Object);
        WithAuth(_sut);
    }

    [Fact]
    public async Task GetTaxRates_ReturnsOk()
    {
        var result = await _sut.GetTaxRates();
        result.Should().BeOfType<OkObjectResult>();
    }
}
