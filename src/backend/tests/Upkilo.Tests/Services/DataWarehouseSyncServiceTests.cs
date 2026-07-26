using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
using Upkilo.Infrastructure.Services;
using Upkilo.Tests.Helpers;
using Xunit;

namespace Upkilo.Tests.Services;

public class DataWarehouseSyncServiceTests : IDisposable
{
    private readonly Mock<ILogger<DataWarehouseSyncService>> _loggerMock = new();
    private readonly Mock<IConnectionMultiplexer> _redisMock = new();
    private readonly Mock<IDatabase> _dbMock = new();
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();
    private readonly Mock<IConfiguration> _configMock = new();
    private readonly TestDbContextFactory _dbFactory = new();

    public DataWarehouseSyncServiceTests()
    {
        _redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_dbMock.Object);
        _configMock.Setup(c => c["DataWarehouse:Target"]).Returns("none");
    }

    private DataWarehouseSyncService CreateSut() =>
        new(_loggerMock.Object, _dbFactory.CreateContext(), _redisMock.Object,
            _httpClientFactoryMock.Object, _configMock.Object);

    [Fact]
    public async Task RunIncrementalSyncAsync_NoTargetConfigured_CompletesWithoutThrow()
    {
        var service = CreateSut();
        var tenantId = Guid.NewGuid();

        var act = () => service.RunIncrementalSyncAsync(tenantId, "bookings");

        await act.Should().NotThrowAsync();
    }

    public void Dispose() => _dbFactory.Dispose();
}
