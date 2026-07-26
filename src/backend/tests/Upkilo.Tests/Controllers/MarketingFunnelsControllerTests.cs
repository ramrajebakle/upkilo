using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Upkilo.API.Controllers;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Core.Interfaces.Workflow;
using WorkflowEntity = Upkilo.Core.Entities.Workflow;
using Upkilo.Infrastructure.Data;
using Upkilo.Tests.Helpers;

namespace Upkilo.Tests.Controllers;

public class MarketingFunnelsControllerTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory;
    private readonly AppDbContext _context;
    private readonly Mock<ITenantProvider> _tenantProvider;
    private readonly Mock<IWorkflowService> _workflowService;
    private readonly Mock<ILogger<MarketingFunnelsController>> _logger;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly MarketingFunnelsController _sut;

    public MarketingFunnelsControllerTests()
    {
        _dbFactory = new TestDbContextFactory();
        _context = _dbFactory.CreateContext();
        _tenantProvider = new Mock<ITenantProvider>();
        _workflowService = new Mock<IWorkflowService>();
        _logger = new Mock<ILogger<MarketingFunnelsController>>();

        _tenantProvider.Setup(t => t.GetTenantId()).Returns(_tenantId);

        // Seed tenant
        _context.Tenants.Add(new Tenant
        {
            Id = _tenantId,
            Name = "Funnel Tenant",
            Slug = "funnel-tenant",
            Status = TenantStatus.Active,
            CreatedAt = DateTime.UtcNow
        });
        _context.SaveChanges();

        _sut = new MarketingFunnelsController(_context, _tenantProvider.Object, _workflowService.Object, _logger.Object);
    }

    public void Dispose()
    {
        _dbFactory.Dispose();
    }

    [Fact]
    public async Task GetFunnels_ReturnsOnlyTenantWorkflows()
    {
        var anotherTenant = Guid.NewGuid();
        _context.Workflows.Add(new WorkflowEntity
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "My Tenant Workflow",
            TriggerType = "ClientCreated"
        });

        _context.Tenants.Add(new Tenant
        {
            Id = anotherTenant,
            Name = "Another Tenant",
            Slug = "another-tenant",
            Status = TenantStatus.Active,
            CreatedAt = DateTime.UtcNow
        });

        _context.Workflows.Add(new WorkflowEntity
        {
            Id = Guid.NewGuid(),
            TenantId = anotherTenant,
            Name = "Other Tenant Workflow",
            TriggerType = "ClientCreated"
        });
        await _context.SaveChangesAsync();

        var response = await _sut.GetFunnels();

        var okResult = response.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetFunnel_ValidId_ReturnsWorkflow()
    {
        var workflowId = Guid.NewGuid();
        var workflow = new WorkflowEntity
        {
            Id = workflowId,
            TenantId = _tenantId,
            Name = "My Workflow",
            TriggerType = "ClientCreated"
        };
        _context.Workflows.Add(workflow);
        await _context.SaveChangesAsync();

        var response = await _sut.GetFunnel(workflowId);

        var okResult = response.Should().BeOfType<OkObjectResult>().Subject;
        var returnedWorkflow = okResult.Value.Should().BeOfType<WorkflowEntity>().Subject;
        returnedWorkflow.Id.Should().Be(workflowId);
    }

    [Fact]
    public async Task SaveFunnel_NewWorkflow_CreatesWorkflow()
    {
        var request = new SaveFunnelRequest(
            Id: null,
            Name: "New Workflow",
            Description: "A test workflow",
            TriggerType: "ClientCreated",
            TriggerConfig: "{}",
            Steps: "[]",
            IsActive: true
        );

        var response = await _sut.SaveFunnel(request);

        var okResult = response.Should().BeOfType<OkObjectResult>().Subject;
        var returnedWorkflow = okResult.Value.Should().BeOfType<WorkflowEntity>().Subject;
        returnedWorkflow.Name.Should().Be("New Workflow");
        _context.Workflows.Should().Contain(w => w.Name == "New Workflow");
    }

    [Fact]
    public async Task DeleteFunnel_SoftDeletesWorkflow()
    {
        var workflowId = Guid.NewGuid();
        var workflow = new WorkflowEntity
        {
            Id = workflowId,
            TenantId = _tenantId,
            Name = "Workflow To Delete",
            TriggerType = "ClientCreated"
        };
        _context.Workflows.Add(workflow);
        await _context.SaveChangesAsync();

        var response = await _sut.DeleteFunnel(workflowId);

        response.Should().BeOfType<NoContentResult>();
        var dbWorkflow = await _context.Workflows.IgnoreQueryFilters().FirstOrDefaultAsync(w => w.Id == workflowId);
        dbWorkflow.Should().NotBeNull();
        dbWorkflow!.IsDeleted.Should().BeTrue();
    }
}
