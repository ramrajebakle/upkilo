using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Services;
using Upkilo.Infrastructure.Services.AI;
using Upkilo.Tests.Helpers;
using Xunit;

namespace Upkilo.Tests.Services;

public class AIAuditServiceTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory;
    private readonly Mock<ILogger<AIAuditService>> _loggerMock;

    public AIAuditServiceTests()
    {
        _dbFactory = new TestDbContextFactory();
        _loggerMock = new Mock<ILogger<AIAuditService>>();
    }

    [Fact]
    public void Constructor_ValidArgs_CreatesInstance()
    {
        using var ctx = _dbFactory.CreateContext();
        var svc = new AIAuditService(ctx, _loggerMock.Object);
        svc.Should().NotBeNull();
    }

    [Fact]
    public async Task LogAsync_ValidEntry_PersistsToDb()
    {
        using var ctx = _dbFactory.CreateContext();
        var svc = new AIAuditService(ctx, _loggerMock.Object);

        var tenantId = Guid.NewGuid();
        var entry = new AIAuditEntry
        {
            TenantId = tenantId,
            Feature = "booking_assistant",
            OriginalPrompt = "Book me an appointment",
            SanitizedPrompt = "Book me an appointment",
            Response = "Appointment booked successfully",
            WasBlocked = false,
            ConfidenceScore = 0.95,
            InputTokens = 10
        };

        await svc.LogAsync(entry);

        var logs = await svc.GetLogsAsync(tenantId);
        logs.Should().NotBeNull();
        logs.Count.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task GetLogsAsync_NoLogs_ReturnsEmpty()
    {
        using var ctx = _dbFactory.CreateContext();
        var svc = new AIAuditService(ctx, _loggerMock.Object);

        var logs = await svc.GetLogsAsync(Guid.NewGuid());

        logs.Should().NotBeNull();
        logs.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPendingApprovalAsync_NoEntries_ReturnsEmpty()
    {
        using var ctx = _dbFactory.CreateContext();
        var svc = new AIAuditService(ctx, _loggerMock.Object);

        var result = await svc.GetPendingApprovalAsync(Guid.NewGuid());

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    public void Dispose() => _dbFactory.Dispose();
}
