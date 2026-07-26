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

public class SandboxServiceTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory;

    public SandboxServiceTests()
    {
        _dbFactory = new TestDbContextFactory();
    }

    [Fact]
    public void Constructor_ValidArgs_CreatesInstance()
    {
        using var ctx = _dbFactory.CreateContext();
        var svc = new SandboxService(ctx);
        svc.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateSandboxAsync_ValidUser_ReturnsSandboxEnvironment()
    {
        using var ctx = _dbFactory.CreateContext();
        var svc = new SandboxService(ctx);

        var userId = Guid.NewGuid();
        var result = await svc.CreateSandboxAsync(userId);

        result.Should().NotBeNull();
        result.Id.Should().NotBe(Guid.Empty);
        result.IsActive.Should().BeTrue();
        result.SandboxId.Should().NotBeNullOrEmpty();
        result.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task IsSandboxValidAsync_UnknownId_ReturnsFalse()
    {
        using var ctx = _dbFactory.CreateContext();
        var svc = new SandboxService(ctx);

        var result = await svc.IsSandboxValidAsync("nonexistent-sandbox-id");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CreateSandboxAsync_ThenDeleteIt_IsNoLongerValid()
    {
        using var ctx = _dbFactory.CreateContext();
        var svc = new SandboxService(ctx);

        var sandbox = await svc.CreateSandboxAsync(Guid.NewGuid());
        var sandboxId = sandbox.SandboxId;

        // Verify it is valid before deletion
        var validBefore = await svc.IsSandboxValidAsync(sandboxId);
        validBefore.Should().BeTrue();

        await svc.DeleteSandboxAsync(sandboxId);

        var validAfter = await svc.IsSandboxValidAsync(sandboxId);
        validAfter.Should().BeFalse();
    }

    public void Dispose() => _dbFactory.Dispose();
}
