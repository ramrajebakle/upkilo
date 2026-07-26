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

public class LiveChatServiceTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory;
    private readonly Mock<ILogger<LiveChatService>> _loggerMock;

    public LiveChatServiceTests()
    {
        _dbFactory = new TestDbContextFactory();
        _loggerMock = new Mock<ILogger<LiveChatService>>();
    }

    [Fact]
    public void Constructor_ValidArgs_CreatesInstance()
    {
        using var ctx = _dbFactory.CreateContext();
        var svc = new LiveChatService(ctx, _loggerMock.Object);
        svc.Should().NotBeNull();
    }

    [Fact]
    public async Task GetCannedResponsesAsync_NoCustomResponses_ReturnsDefaultResponses()
    {
        using var ctx = _dbFactory.CreateContext();
        var svc = new LiveChatService(ctx, _loggerMock.Object);

        var result = await svc.GetCannedResponsesAsync(Guid.NewGuid());

        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
        result.Count.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task SubmitPreChatFormAsync_UnknownEmail_CreatesConversationWithoutThrow()
    {
        using var ctx = _dbFactory.CreateContext();
        var svc = new LiveChatService(ctx, _loggerMock.Object);

        var tenantId = Guid.NewGuid();
        var form = new PreChatFormData
        {
            Name = "New User",
            Email = "newuser@example.com",
            Subject = "Help needed"
        };

        var result = await svc.SubmitPreChatFormAsync(tenantId, form);

        result.Should().NotBeNull();
        result.ConversationId.Should().NotBe(Guid.Empty);
        result.IsReturningClient.Should().BeFalse();
    }

    [Fact]
    public async Task SubmitPreChatFormAsync_KnownEmail_SetsIsReturningClient()
    {
        using var ctx = _dbFactory.CreateContext();
        var tenantId = Guid.NewGuid();

        ctx.Clients.Add(new Client
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FirstName = "Jane",
            LastName = "Doe",
            Email = "jane@example.com"
        });
        ctx.SaveChanges();

        var svc = new LiveChatService(ctx, _loggerMock.Object);
        var form = new PreChatFormData { Name = "Jane Doe", Email = "jane@example.com" };

        var result = await svc.SubmitPreChatFormAsync(tenantId, form);

        result.IsReturningClient.Should().BeTrue();
        result.ClientId.Should().NotBeNull();
    }

    public void Dispose() => _dbFactory.Dispose();
}
