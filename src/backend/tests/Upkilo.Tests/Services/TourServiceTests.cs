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

public class TourServiceTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory;

    public TourServiceTests()
    {
        _dbFactory = new TestDbContextFactory();
    }

    [Fact]
    public void Constructor_ValidArgs_CreatesInstance()
    {
        using var ctx = _dbFactory.CreateContext();
        var svc = new TourService(ctx);
        svc.Should().NotBeNull();
    }

    [Fact]
    public async Task GetProgressAsync_NewUser_ReturnsInitialProgress()
    {
        using var ctx = _dbFactory.CreateContext();
        var svc = new TourService(ctx);

        var userId = Guid.NewGuid();
        var result = await svc.GetProgressAsync(userId, "onboarding");

        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.TourKey.Should().Be("onboarding");
        result.CurrentStep.Should().Be(0);
        result.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public async Task CompleteStepAsync_ValidStep_UpdatesProgress()
    {
        using var ctx = _dbFactory.CreateContext();
        var svc = new TourService(ctx);

        var userId = Guid.NewGuid();

        // Ensure initial progress exists
        await svc.GetProgressAsync(userId, "onboarding");

        // Advance to step 2 and mark completed
        await svc.UpdateProgressAsync(userId, "onboarding", 2, completed: false);

        var progress = await svc.GetProgressAsync(userId, "onboarding");
        progress.CurrentStep.Should().Be(2);
        progress.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateProgressAsync_WithCompleted_SetsCompletedAt()
    {
        using var ctx = _dbFactory.CreateContext();
        var svc = new TourService(ctx);

        var userId = Guid.NewGuid();
        await svc.GetProgressAsync(userId, "setup");
        await svc.UpdateProgressAsync(userId, "setup", 5, completed: true);

        var progress = await svc.GetProgressAsync(userId, "setup");
        progress.IsCompleted.Should().BeTrue();
        progress.CompletedAt.Should().NotBeNull();
    }

    public void Dispose() => _dbFactory.Dispose();
}
