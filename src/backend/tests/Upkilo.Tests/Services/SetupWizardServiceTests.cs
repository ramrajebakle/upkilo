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

public class SetupWizardServiceTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory;
    private readonly Mock<ILogger<SetupWizardService>> _loggerMock;

    public SetupWizardServiceTests()
    {
        _dbFactory = new TestDbContextFactory();
        _loggerMock = new Mock<ILogger<SetupWizardService>>();
    }

    [Fact]
    public void Constructor_ValidArgs_CreatesInstance()
    {
        using var ctx = _dbFactory.CreateContext();
        var svc = new SetupWizardService(ctx, _loggerMock.Object);
        svc.Should().NotBeNull();
    }

    [Fact]
    public async Task GetProgressAsync_NewTenant_ReturnsInitialProgress()
    {
        using var ctx = _dbFactory.CreateContext();
        var svc = new SetupWizardService(ctx, _loggerMock.Object);

        var tenantId = Guid.NewGuid();
        var result = await svc.GetProgressAsync(tenantId);

        result.Should().NotBeNull();
        result.TenantId.Should().Be(tenantId);
        result.ProfileCompleted.Should().BeFalse();
        result.ServicesCompleted.Should().BeFalse();
        result.StaffCompleted.Should().BeFalse();
    }

    [Fact]
    public async Task CompleteStepAsync_ProfileStep_SetsProfileCompleted()
    {
        using var ctx = _dbFactory.CreateContext();
        var svc = new SetupWizardService(ctx, _loggerMock.Object);

        var tenantId = Guid.NewGuid();
        var result = await svc.CompleteStepAsync(tenantId, "profile");

        result.Should().NotBeNull();
        result.ProfileCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task CompleteStepAsync_MultipleSteps_UpdatesAllCompletedSteps()
    {
        using var ctx = _dbFactory.CreateContext();
        var svc = new SetupWizardService(ctx, _loggerMock.Object);

        var tenantId = Guid.NewGuid();
        await svc.CompleteStepAsync(tenantId, "profile");
        await svc.CompleteStepAsync(tenantId, "services");
        var result = await svc.CompleteStepAsync(tenantId, "staff");

        result.ProfileCompleted.Should().BeTrue();
        result.ServicesCompleted.Should().BeTrue();
        result.StaffCompleted.Should().BeTrue();
        result.AvailabilityCompleted.Should().BeFalse();
    }

    public void Dispose() => _dbFactory.Dispose();
}
