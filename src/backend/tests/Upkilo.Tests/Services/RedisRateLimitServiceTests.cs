using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using Upkilo.Infrastructure.Services;
using Xunit;

namespace Upkilo.Tests.Services;

public class RedisRateLimitServiceTests
{
    private readonly Mock<IDistributedCache> _cacheMock = new();
    private readonly Mock<ILogger<RedisRateLimitService>> _loggerMock = new();

    private RedisRateLimitService CreateSut() =>
        new RedisRateLimitService(_cacheMock.Object, _loggerMock.Object);

    [Fact]
    public async Task IsAllowedAsync_WhenUnderLimit_ReturnsTrue()
    {
        // Cache returns count of 3 for limit of 5
        _cacheMock.Setup(c => c.GetAsync("rl:tenant1", default))
            .ReturnsAsync(System.Text.Encoding.UTF8.GetBytes("3"));

        var sut = CreateSut();
        var result = await sut.IsAllowedAsync("tenant1", limit: 5, TimeSpan.FromMinutes(1));

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsAllowedAsync_WhenAtLimit_ReturnsFalse()
    {
        // Cache returns count = limit
        _cacheMock.Setup(c => c.GetAsync("rl:tenant1", default))
            .ReturnsAsync(System.Text.Encoding.UTF8.GetBytes("5"));

        var sut = CreateSut();
        var result = await sut.IsAllowedAsync("tenant1", limit: 5, TimeSpan.FromMinutes(1));

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsAllowedAsync_WhenCacheEmpty_CountsAsFirstRequest_ReturnsTrue()
    {
        // No cache entry = first request
        _cacheMock.Setup(c => c.GetAsync("rl:newkey", default))
            .ReturnsAsync((byte[]?)null);

        var sut = CreateSut();
        var result = await sut.IsAllowedAsync("newkey", limit: 5, TimeSpan.FromMinutes(1));

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsAllowedAsync_WhenCacheThrows_FailsOpen()
    {
        // On cache failure, the service should fail open (allow the request)
        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), default))
            .ThrowsAsync(new Exception("Redis connection refused"));

        var sut = CreateSut();
        var result = await sut.IsAllowedAsync("failkey", limit: 5, TimeSpan.FromMinutes(1));

        result.Should().BeTrue(); // Fail-open for resilience
    }
}
