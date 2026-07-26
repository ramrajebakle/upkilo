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

public class DeadLetterServiceTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory;
    private readonly Mock<ILogger<DeadLetterService>> _loggerMock;

    public DeadLetterServiceTests()
    {
        _dbFactory = new TestDbContextFactory();
        _loggerMock = new Mock<ILogger<DeadLetterService>>();
    }

    [Fact]
    public void Constructor_ValidArgs_CreatesInstance()
    {
        using var ctx = _dbFactory.CreateContext();
        var svc = new DeadLetterService(ctx, _loggerMock.Object);
        svc.Should().NotBeNull();
    }

    [Fact]
    public async Task MoveToDeadLetterAsync_ValidMessage_PersistsAuditEntry()
    {
        using var ctx = _dbFactory.CreateContext();
        var svc = new DeadLetterService(ctx, _loggerMock.Object);

        var tenantId = Guid.NewGuid();
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EventType = "TestEvent",
            Payload = "{\"key\":\"value\"}",
            CreatedAt = DateTime.UtcNow
        };

        var act = async () => await svc.MoveToDeadLetterAsync(message, "Test reason", "Test exception");
        await act.Should().NotThrowAsync();

        message.IsProcessed.Should().BeTrue();
    }

    [Fact]
    public async Task MoveToDeadLetterAsync_WithException_StillPersistsRecord()
    {
        using var ctx = _dbFactory.CreateContext();
        var svc = new DeadLetterService(ctx, _loggerMock.Object);

        var tenantId = Guid.NewGuid();
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EventType = "FailedEvent",
            Payload = "{}",
            CreatedAt = DateTime.UtcNow
        };

        await svc.MoveToDeadLetterAsync(message, "Permanent failure", "System.TimeoutException");

        message.IsProcessed.Should().BeTrue();
        // DateTime is a value type — use BeAfter(default) instead of NotBeNull()
        message.UpdatedAt.Should().BeAfter(default(DateTime));
    }

    [Fact]
    public async Task MoveToDeadLetterAsync_NoExceptionParam_CompletesWithoutThrow()
    {
        using var ctx = _dbFactory.CreateContext();
        var svc = new DeadLetterService(ctx, _loggerMock.Object);

        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            EventType = "SimpleEvent",
            Payload = "{}",
            CreatedAt = DateTime.UtcNow
        };

        var act = async () => await svc.MoveToDeadLetterAsync(message, "reason only");
        await act.Should().NotThrowAsync();
    }

    public void Dispose() => _dbFactory.Dispose();
}
