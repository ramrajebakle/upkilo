using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Upkilo.Infrastructure.Services;
using Upkilo.Tests.Helpers;
using Xunit;

namespace Upkilo.Tests.Services;

public class ComplianceReportServiceTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory;
    private readonly Mock<ILogger<ComplianceReportService>> _loggerMock = new();

    public ComplianceReportServiceTests() => _dbFactory = new TestDbContextFactory();
    public void Dispose() => _dbFactory.Dispose();

    [Fact]
    public async Task GenerateReportAsync_WithNoData_ReturnsZeroCountReport()
    {
        var ctx = _dbFactory.CreateContext();
        var sut = new ComplianceReportService(ctx, _loggerMock.Object);
        var tenantId = Guid.NewGuid();
        var from = DateTime.UtcNow.AddDays(-30);
        var to = DateTime.UtcNow;

        var report = await sut.GenerateReportAsync(tenantId, from, to);

        report.Should().NotBeNull();
        report.TenantId.Should().Be(tenantId);
        report.ConsentRecords.Should().Be(0);
        report.AuditLogEntries.Should().Be(0);
        report.SecurityEvents.Should().Be(0);
    }

    [Fact]
    public async Task GenerateReportAsync_SetsPeriodBounds()
    {
        var ctx = _dbFactory.CreateContext();
        var sut = new ComplianceReportService(ctx, _loggerMock.Object);
        var from = new DateTime(2024, 1, 1);
        var to = new DateTime(2024, 6, 30);

        var report = await sut.GenerateReportAsync(Guid.NewGuid(), from, to);

        report.PeriodStart.Should().Be(from);
        report.PeriodEnd.Should().Be(to);
        report.GeneratedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GenerateReportAsync_CountsAuditEntries()
    {
        var ctx = _dbFactory.CreateContext();
        var sut = new ComplianceReportService(ctx, _loggerMock.Object);
        var tenantId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        ctx.AuditEntries.Add(new Upkilo.Core.Entities.AuditEntry
        {
            TenantId = tenantId,
            EntityType = "Booking",
            EntityId = "1",
            Action = "Create",
            Timestamp = now
        });
        await ctx.SaveChangesAsync();

        var report = await sut.GenerateReportAsync(tenantId, now.AddHours(-1), now.AddHours(1));

        report.AuditLogEntries.Should().Be(1);
    }
}
