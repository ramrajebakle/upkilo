using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
using Upkilo.Infrastructure.Services;
using Upkilo.Tests.Helpers;
using Xunit;

namespace Upkilo.Tests.Services;

public class DeveloperPortalServiceTests : IDisposable
{
    private readonly Mock<ILogger<DeveloperPortalService>> _loggerMock = new();
    private readonly Mock<IConnectionMultiplexer> _redisMock = new();
    private readonly Mock<IDatabase> _dbMock = new();
    private readonly TestDbContextFactory _dbFactory = new();

    public DeveloperPortalServiceTests()
    {
        _redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_dbMock.Object);
        _dbMock.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
               .ReturnsAsync(RedisValue.Null);
    }

    private DeveloperPortalService CreateSut() =>
        new(_loggerMock.Object, _dbFactory.CreateContext(), _redisMock.Object);

    [Fact]
    public async Task GetApiMetricsAsync_NoHistoricalData_ReturnsZeroRequests()
    {
        using var ctx = _dbFactory.CreateContext();
        var keyId = Guid.NewGuid();
        ctx.ApiKeys.Add(new Upkilo.Core.Entities.ApiKey
        {
            Id = keyId,
            TenantId = Guid.NewGuid(),
            Name = "Test Key",
            Prefix = "upk_test_",
            KeyHash = "hash",
            LastFourChars = "1234",
            IsActive = true
        });
        await ctx.SaveChangesAsync();

        var service = new DeveloperPortalService(_loggerMock.Object, ctx, _redisMock.Object);

        var result = await service.GetApiMetricsAsync(keyId);

        result.Should().NotBeNull();
        result.TotalRequests.Should().Be(0);
        result.QuotaRemaining.Should().Be(20000);
    }

    [Fact]
    public async Task ProvisionSandboxAsync_ReturnsSandboxEnvironmentName()
    {
        using var ctx = _dbFactory.CreateContext();
        var service = new DeveloperPortalService(_loggerMock.Object, ctx, _redisMock.Object);
        var tenantId = Guid.NewGuid();

        var result = await service.ProvisionSandboxAsync(tenantId);

        result.Should().NotBeNullOrWhiteSpace();
        result.Should().StartWith("sandbox_");
    }

    public void Dispose() => _dbFactory.Dispose();
}
