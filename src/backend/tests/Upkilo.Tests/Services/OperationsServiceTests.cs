using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Upkilo.Infrastructure.Services;
using Upkilo.Tests.Helpers;
using Xunit;

namespace Upkilo.Tests.Services;

public class OperationsServiceTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory = new();
    private readonly Mock<ILogger<OperationsService>> _loggerMock = new();

    [Fact]
    public async Task GetQuotasAsync_NewTenant_ReturnsDefaults()
    {
        await using var context = _dbFactory.CreateContext();
        var service = new OperationsService(context, _loggerMock.Object);
        var tenantId = Guid.NewGuid();

        var quota = await service.GetQuotasAsync(tenantId);

        quota.Should().NotBeNull();
        quota.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public async Task GetQuotasAsync_SameTenantTwice_ReturnsSameRecord()
    {
        await using var context = _dbFactory.CreateContext();
        var service = new OperationsService(context, _loggerMock.Object);
        var tenantId = Guid.NewGuid();

        var first = await service.GetQuotasAsync(tenantId);
        var second = await service.GetQuotasAsync(tenantId);

        second.TenantId.Should().Be(first.TenantId);
    }

    public void Dispose() => _dbFactory.Dispose();
}
