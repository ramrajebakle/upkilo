using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Services;
using Upkilo.Tests.Helpers;
using Xunit;

namespace Upkilo.Tests.Services;

public class ReportingServiceTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<ILogger<ReportingService>> _loggerMock;

    public ReportingServiceTests()
    {
        _dbFactory = new TestDbContextFactory();
        _emailServiceMock = new Mock<IEmailService>();
        _loggerMock = new Mock<ILogger<ReportingService>>();
    }

    [Fact]
    public void Constructor_ValidArgs_CreatesInstance()
    {
        using var ctx = _dbFactory.CreateContext();
        var svc = new ReportingService(ctx, _emailServiceMock.Object, _loggerMock.Object);
        svc.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteReportAsync_ClientsReportEmptyDb_ReturnsEmptyRows()
    {
        using var ctx = _dbFactory.CreateContext();
        var svc = new ReportingService(ctx, _emailServiceMock.Object, _loggerMock.Object);

        var tenantId = Guid.NewGuid();
        var definition = new ReportDefinition
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "Clients Report",
            ReportType = "clients",
            ConfigJson = "{}"
        };

        var result = await svc.ExecuteReportAsync(tenantId, definition);

        result.Should().NotBeNull();
        result.Rows.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteReportAsync_InvalidReportType_ThrowsArgumentException()
    {
        using var ctx = _dbFactory.CreateContext();
        var svc = new ReportingService(ctx, _emailServiceMock.Object, _loggerMock.Object);

        var definition = new ReportDefinition
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Name = "Bad Report",
            ReportType = "unknown_type",
            ConfigJson = "{}"
        };

        var act = async () => await svc.ExecuteReportAsync(Guid.NewGuid(), definition);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ProcessScheduledReportsAsync_NoScheduledReports_CompletesWithoutThrow()
    {
        using var ctx = _dbFactory.CreateContext();
        var svc = new ReportingService(ctx, _emailServiceMock.Object, _loggerMock.Object);

        var act = async () => await svc.ProcessScheduledReportsAsync();
        await act.Should().NotThrowAsync();
    }

    public void Dispose() => _dbFactory.Dispose();
}
