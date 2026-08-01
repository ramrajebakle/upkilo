using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Services;
using Xunit;

namespace Upkilo.Tests.Services;

public class CacheServiceTests
{
    private readonly Mock<IDistributedCache> _cacheMock = new();
    private readonly Mock<IBusinessMetrics> _metricsMock = new();
    private readonly Mock<ILogger<CacheService>> _loggerMock = new();
    private readonly Mock<IConnectionMultiplexer> _redisMock = new();
    private readonly Mock<IDatabase> _dbMock = new();
    private readonly Mock<IServer> _serverMock = new();

    private CacheService CreateSut(bool includeRedis = true) =>
        new CacheService(
            _cacheMock.Object,
            _metricsMock.Object,
            _loggerMock.Object,
            includeRedis ? _redisMock.Object : null
        );

    [Fact]
    public async Task GetOrSetAsync_CacheHit_ReturnsCachedValue()
    {
        var tenantId = Guid.NewGuid();
        var key = "test-key";
        var cacheKey = $"t:{tenantId}:{key}";
        var cachedData = "Hello World";
        var json = JsonSerializer.Serialize(cachedData);

        _cacheMock.Setup(c => c.GetAsync(cacheKey, default))
            .ReturnsAsync(System.Text.Encoding.UTF8.GetBytes(json));

        var sut = CreateSut(includeRedis: false);

        var result = await sut.GetOrSetAsync(tenantId, key, async () => "Fallback", TimeSpan.FromMinutes(5));

        result.Should().Be("Hello World");
        _metricsMock.Verify(m => m.RecordCacheHit(key), Times.Once);
        _metricsMock.Verify(m => m.RecordCacheMiss(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetOrSetAsync_CacheMiss_CallsFactoryAndSetsCache()
    {
        var tenantId = Guid.NewGuid();
        var key = "test-key";
        var cacheKey = $"t:{tenantId}:{key}";

        _cacheMock.Setup(c => c.GetAsync(cacheKey, default))
            .ReturnsAsync((byte[]?)null);

        var sut = CreateSut(includeRedis: false);

        var factoryCalled = false;
        var result = await sut.GetOrSetAsync(tenantId, key, async () =>
        {
            factoryCalled = true;
            return "FactoryValue";
        }, TimeSpan.FromMinutes(5));

        result.Should().Be("FactoryValue");
        factoryCalled.Should().BeTrue();
        _metricsMock.Verify(m => m.RecordCacheMiss(key), Times.Once);

        _cacheMock.Verify(c => c.SetAsync(
            cacheKey,
            It.IsAny<byte[]>(),
            It.Is<DistributedCacheEntryOptions>(o => o.AbsoluteExpirationRelativeToNow != null),
            default
        ), Times.Once);
    }

    [Fact]
    public async Task RemoveAndInvalidateAsync_CallsCacheRemove()
    {
        var tenantId = Guid.NewGuid();
        var key = "test-key";
        var cacheKey = $"t:{tenantId}:{key}";

        var sut = CreateSut(includeRedis: false);

        await sut.InvalidateAsync(tenantId, key);
        await sut.RemoveAsync(key);

        _cacheMock.Verify(c => c.RemoveAsync(cacheKey, default), Times.Once);
        _cacheMock.Verify(c => c.RemoveAsync("t:00000000-0000-0000-0000-000000000000:test-key", default), Times.Once);
    }

    [Fact]
    public async Task InvalidatePatternAsync_RedisNull_LogsWarning()
    {
        var sut = CreateSut(includeRedis: false);
        await sut.InvalidatePatternAsync(Guid.NewGuid(), "prefix");

        // Assert: No exceptions, should log warning
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("IConnectionMultiplexer is not registered")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ), Times.Once);
    }

    [Fact]
    public async Task InvalidatePatternAsync_RedisRegistered_DeletesMatchingKeys()
    {
        var tenantId = Guid.NewGuid();
        var prefix = "prefix";
        var pattern = $"t:{tenantId}:{prefix}*";

        var endpointMock = new Mock<EndPoint>();
        var endpoints = new[] { endpointMock.Object };

        _redisMock.Setup(r => r.GetEndPoints(false)).Returns(endpoints);
        _redisMock.Setup(r => r.GetServer(endpointMock.Object, null)).Returns(_serverMock.Object);
        _redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_dbMock.Object);

        var keys = new RedisKey[] { "t:tenantId:prefix:1", "t:tenantId:prefix:2" };
        _serverMock.Setup(s => s.Keys(It.IsAny<int>(), It.Is<RedisValue>(v => v.ToString() == pattern), It.IsAny<int>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<CommandFlags>()))
            .Returns(keys);

        var sut = CreateSut(includeRedis: true);

        await sut.InvalidatePatternAsync(tenantId, prefix);

        _dbMock.Verify(db => db.KeyDeleteAsync(keys, CommandFlags.None), Times.Once);
    }
}
