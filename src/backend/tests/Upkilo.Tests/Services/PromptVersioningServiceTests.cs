using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Services;
using Upkilo.Infrastructure.Services.Security;
using Upkilo.Tests.Helpers;
using Xunit;

namespace Upkilo.Tests.Services;

public class PromptVersioningServiceTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory;
    private readonly Mock<ILogger<PromptVersioningService>> _loggerMock;

    public PromptVersioningServiceTests()
    {
        _dbFactory = new TestDbContextFactory();
        _loggerMock = new Mock<ILogger<PromptVersioningService>>();
    }

    [Fact]
    public void Constructor_ValidArgs_CreatesInstance()
    {
        using var ctx = _dbFactory.CreateContext();
        var svc = new PromptVersioningService(ctx, _loggerMock.Object);
        svc.Should().NotBeNull();
    }

    [Fact]
    public async Task GetActivePromptAsync_NoVersions_ReturnsNull()
    {
        using var ctx = _dbFactory.CreateContext();
        var svc = new PromptVersioningService(ctx, _loggerMock.Object);

        var result = await svc.GetActivePromptAsync("booking_assistant", Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateVersionAsync_ValidInput_PersistsVersion()
    {
        using var ctx = _dbFactory.CreateContext();
        var svc = new PromptVersioningService(ctx, _loggerMock.Object);

        var tenantId = Guid.NewGuid();
        var newVersion = new PromptVersion
        {
            TenantId = tenantId,
            PromptKey = "booking_assistant",
            Version = "1.0.0",
            SystemPrompt = "You are a helpful booking assistant.",
            Model = "gpt-4",
            IsActive = true
        };

        var result = await svc.CreateVersionAsync(newVersion);

        result.Should().NotBeNull();
        result.Id.Should().NotBe(Guid.Empty);
        result.IsActive.Should().BeTrue();

        var retrieved = await svc.GetActivePromptAsync("booking_assistant", tenantId);
        retrieved.Should().NotBeNull();
        retrieved!.Version.Should().Be("1.0.0");
    }

    [Fact]
    public async Task GetVersionHistoryAsync_NoVersions_ReturnsEmpty()
    {
        using var ctx = _dbFactory.CreateContext();
        var svc = new PromptVersioningService(ctx, _loggerMock.Object);

        var result = await svc.GetVersionHistoryAsync("unknown_key", Guid.NewGuid());

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    public void Dispose() => _dbFactory.Dispose();
}
