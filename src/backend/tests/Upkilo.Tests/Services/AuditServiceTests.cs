using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Upkilo.Infrastructure.Services;
using Upkilo.Tests.Helpers;
using Xunit;

namespace Upkilo.Tests.Services;

public class AuditServiceTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory;
    private readonly Mock<ILogger<AuditService>> _loggerMock = new();
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock = new();

    public AuditServiceTests()
    {
        _dbFactory = new TestDbContextFactory();
        _httpContextAccessorMock.Setup(h => h.HttpContext).Returns((HttpContext?)null);
    }

    public void Dispose() => _dbFactory.Dispose();

    private AuditService CreateSut() =>
        new AuditService(_dbFactory.CreateContext(), _loggerMock.Object, _httpContextAccessorMock.Object);

    [Fact]
    public async Task LogAsync_PersistsAuditEntryToDatabase()
    {
        // Arrange
        var ctx = _dbFactory.CreateContext();
        var sut = new AuditService(ctx, _loggerMock.Object, _httpContextAccessorMock.Object);
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // Act
        await sut.LogAsync(tenantId, userId, "Booking", "bk-001", "Create",
            oldValues: null, newValues: new { Status = "Confirmed" });

        // Assert
        ctx.ChangeTracker.Clear();
        var entries = ctx.AuditEntries.ToList();
        entries.Should().HaveCount(1);
        entries[0].TenantId.Should().Be(tenantId);
        entries[0].UserId.Should().Be(userId);
        entries[0].EntityType.Should().Be("Booking");
        entries[0].EntityId.Should().Be("bk-001");
        entries[0].Action.Should().Be("Create");
        entries[0].OldValues.Should().BeNull();
        entries[0].NewValues.Should().Contain("Confirmed");
    }

    [Fact]
    public async Task GetLogsAsync_FiltersCorrectlyByTenantAndEntityType()
    {
        // Arrange
        var ctx = _dbFactory.CreateContext();
        var sut = new AuditService(ctx, _loggerMock.Object, _httpContextAccessorMock.Object);
        var tenantId = Guid.NewGuid();
        var otherTenant = Guid.NewGuid();

        await sut.LogAsync(tenantId, null, "Booking", "1", "Create", null, null);
        await sut.LogAsync(tenantId, null, "Client", "2", "Update", null, null);
        await sut.LogAsync(otherTenant, null, "Booking", "3", "Create", null, null);

        // Act
        var logs = await sut.GetLogsAsync(tenantId, entityType: "Booking");

        // Assert
        logs.Should().HaveCount(1);
        logs.First().EntityType.Should().Be("Booking");
    }

    [Fact]
    public async Task GetLogsAsync_RespectsLimit()
    {
        // Arrange
        var ctx = _dbFactory.CreateContext();
        var sut = new AuditService(ctx, _loggerMock.Object, _httpContextAccessorMock.Object);
        var tenantId = Guid.NewGuid();

        for (int i = 0; i < 10; i++)
            await sut.LogAsync(tenantId, null, "Booking", i.ToString(), "Create", null, null);

        // Act
        var logs = await sut.GetLogsAsync(tenantId, limit: 3);

        // Assert
        logs.Should().HaveCount(3);
    }

    [Fact]
    public async Task ExportToJsonAsync_ReturnsNonEmptyBytes()
    {
        // Arrange
        var ctx = _dbFactory.CreateContext();
        var sut = new AuditService(ctx, _loggerMock.Object, _httpContextAccessorMock.Object);
        var tenantId = Guid.NewGuid();
        await sut.LogAsync(tenantId, null, "Test", "1", "Delete", null, null);

        // Act
        var bytes = await sut.ExportToJsonAsync(tenantId);

        // Assert
        bytes.Should().NotBeEmpty();
        System.Text.Encoding.UTF8.GetString(bytes).Should().Contain("Test");
    }
}
