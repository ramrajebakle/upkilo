using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
using Upkilo.Infrastructure.Services;
using Xunit;

namespace Upkilo.Tests.Services;

public class RedisCacheServiceTests
{
    private readonly Mock<IDistributedCache> _cacheMock = new();
    private readonly Mock<IConnectionMultiplexer> _redisMock = new();
    private readonly Mock<IDatabase> _dbMock = new();
    private readonly Mock<ILogger<RedisCacheService>> _loggerMock = new();

    public RedisCacheServiceTests()
    {
        _redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_dbMock.Object);
    }

    private RedisCacheService CreateSut() =>
        new RedisCacheService(_cacheMock.Object, _redisMock.Object, _loggerMock.Object);

    [Fact]
    public async Task GetAsync_ReturnsDeserializedObject()
    {
        var key = "test-key";
        var obj = new TestCacheObj { Name = "Upkilo" };
        var json = JsonSerializer.Serialize(obj);

        _cacheMock.Setup(c => c.GetAsync(key, default))
            .ReturnsAsync(System.Text.Encoding.UTF8.GetBytes(json));

        var sut = CreateSut();
        var result = await sut.GetAsync<TestCacheObj>(key);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Upkilo");
    }

    [Fact]
    public async Task SetAsync_SerializesAndSetsInCache()
    {
        var key = "test-key";
        var obj = new TestCacheObj { Name = "Upkilo" };

        var sut = CreateSut();
        await sut.SetAsync(key, obj, TimeSpan.FromMinutes(5));

        _cacheMock.Verify(c => c.SetAsync(
            key,
            It.IsAny<byte[]>(),
            It.Is<DistributedCacheEntryOptions>(o => o.AbsoluteExpirationRelativeToNow != null),
            default
        ), Times.Once);
    }

    [Fact]
    public async Task RemoveAsync_RemovesFromCache()
    {
        var key = "test-key";
        var sut = CreateSut();
        await sut.RemoveAsync(key);

        _cacheMock.Verify(c => c.RemoveAsync(key, default), Times.Once);
    }

    [Fact]
    public async Task GetOrSetAsync_CacheHit_ReturnsCached()
    {
        var key = "test-key";
        var obj = new TestCacheObj { Name = "Upkilo" };
        var json = JsonSerializer.Serialize(obj);

        _cacheMock.Setup(c => c.GetAsync(key, default))
            .ReturnsAsync(System.Text.Encoding.UTF8.GetBytes(json));

        var sut = CreateSut();
        var factoryCalled = false;
        var result = await sut.GetOrSetAsync(key, async () =>
        {
            factoryCalled = true;
            return new TestCacheObj { Name = "Fallback" };
        });

        result.Name.Should().Be("Upkilo");
        factoryCalled.Should().BeFalse();
    }

    [Fact]
    public async Task GetOrSetAsync_CacheMiss_CallsFactoryAndSets()
    {
        var key = "test-key";
        _cacheMock.Setup(c => c.GetAsync(key, default))
            .ReturnsAsync((byte[]?)null);

        var sut = CreateSut();
        var factoryCalled = false;
        var result = await sut.GetOrSetAsync(key, async () =>
        {
            factoryCalled = true;
            return new TestCacheObj { Name = "Fallback" };
        }, TimeSpan.FromMinutes(10));

        result.Name.Should().Be("Fallback");
        factoryCalled.Should().BeTrue();
    }

    [Fact]
    public async Task AcquireLockAsync_AcquiresAndDisposesRedisLock()
    {
        var key = "test-lock";
        var lockKey = $"lock:{key}";

        // Set up both overloads; v2.7.10 may route through either depending on whether
        // the 4-param default interface method delegates internally to the 6-param version
        _dbMock.Setup(db => db.StringSetAsync(
            It.Is<RedisKey>(k => k == (RedisKey)lockKey),
            It.IsAny<RedisValue>(),
            It.IsAny<TimeSpan?>(),
            When.NotExists
        )).ReturnsAsync(true);
        _dbMock.Setup(db => db.StringSetAsync(
            It.Is<RedisKey>(k => k == (RedisKey)lockKey),
            It.IsAny<RedisValue>(),
            It.IsAny<TimeSpan?>(),
            It.IsAny<bool>(),
            When.NotExists,
            It.IsAny<CommandFlags>()
        )).ReturnsAsync(true);

        var sut = CreateSut();
        var handle = await sut.AcquireLockAsync(key, TimeSpan.FromSeconds(5));

        handle.Should().NotBeNull();

        // Release lock / Dispose
        _dbMock.Setup(db => db.ScriptEvaluateAsync(
            It.IsAny<string>(),
            It.Is<RedisKey[]>(k => k[0] == lockKey),
            It.IsAny<RedisValue[]>(),
            CommandFlags.None
        )).ReturnsAsync(RedisResult.Create((long)1));

        await handle!.DisposeAsync();

        _dbMock.Verify(db => db.ScriptEvaluateAsync(
            It.IsAny<string>(),
            It.Is<RedisKey[]>(k => k[0] == lockKey),
            It.IsAny<RedisValue[]>(),
            CommandFlags.None
        ), Times.Once);
    }

    [Fact]
    public async Task AcquireLockAsync_WhenFailedToAcquire_ReturnsNull()
    {
        var key = "test-lock";
        var lockKey = $"lock:{key}";

        _dbMock.Setup(db => db.StringSetAsync(
            lockKey,
            It.IsAny<RedisValue>(),
            TimeSpan.FromSeconds(5),
            When.NotExists,
            CommandFlags.None
        )).ReturnsAsync(false);

        var sut = CreateSut();
        var handle = await sut.AcquireLockAsync(key, TimeSpan.FromSeconds(5));

        handle.Should().BeNull();
    }

    private class TestCacheObj
    {
        public string Name { get; set; } = string.Empty;
    }
}
