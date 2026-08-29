using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Services;
using Upkilo.Tests.Helpers;
using Xunit;

namespace Upkilo.Tests.Services;

public class SessionServiceTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory;
    private readonly Mock<ILogger<SessionService>> _loggerMock = new();
    private readonly Mock<IConnectionMultiplexer> _redisMock = new();
    private readonly Mock<IDatabase> _dbMock = new();

    public SessionServiceTests()
    {
        _dbFactory = new TestDbContextFactory();
        _redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_dbMock.Object);
    }

    public void Dispose() => _dbFactory.Dispose();

    private SessionService CreateSut()
    {
        return new SessionService(_dbFactory.CreateContext(), _loggerMock.Object, _redisMock.Object);
    }

    [Fact]
    public async Task CreateSessionAsync_SavesToDatabaseAndRedis()
    {
        // Arrange
        var sut = CreateSut();
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var refreshToken = "some-refresh-token";
        var userAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 14_0 like Mac OS X) AppleWebKit/605.1.15 Chrome/85.0.4183.121 Mobile Safari/605.1.15";
        var ip = "127.0.0.1";

        // Act
        var session = await sut.CreateSessionAsync(userId, tenantId, refreshToken, ip, userAgent);

        // Assert
        session.Should().NotBeNull();
        session.UserId.Should().Be(userId);
        session.TenantId.Should().Be(tenantId);
        session.RefreshToken.Should().Be(refreshToken);
        session.IpAddress.Should().Be(ip);
        session.DeviceType.Should().Be("mobile");
        session.Browser.Should().Be("Chrome");
        session.OperatingSystem.Should().Be("iOS");

        // Verify DB contains it
        using (var checkContext = _dbFactory.CreateContext())
        {
            var dbSession = checkContext.Set<UserSession>().Find(session.Id);
            dbSession.Should().NotBeNull();
            dbSession!.UserId.Should().Be(userId);
        }

        // Verify Redis calls
        // SessionService calls StringSetAsync(key, value, TimeSpan), which binds to
        // StringSetAsync(RedisKey, RedisValue, Expiration, ValueCondition, CommandFlags).
        // These assertions previously described (…, TimeSpan?, bool, When, CommandFlags) —
        // a real but never-invoked overload — so Moq matched nothing and both tests failed
        // while the code under test was correct.
        _dbMock.Verify(d => d.StringSetAsync(
            It.Is<RedisKey>(k => k.ToString().Contains(session.Id.ToString())),
            It.IsAny<RedisValue>(),
            It.IsAny<Expiration>(),
            It.IsAny<ValueCondition>(),
            It.IsAny<CommandFlags>()
        ), Times.Once);

        _dbMock.Verify(d => d.StringSetAsync(
            It.Is<RedisKey>(k => k.ToString().Contains(refreshToken)),
            It.Is<RedisValue>(v => v.ToString() == session.Id.ToString()),
            It.IsAny<Expiration>(),
            It.IsAny<ValueCondition>(),
            It.IsAny<CommandFlags>()
        ), Times.Once);
    }

    [Fact]
    public async Task GetActiveSessionsAsync_ReturnsOnlyUnexpiredAndUnrevoked()
    {
        // Arrange
        var context = _dbFactory.CreateContext();
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var active1 = new UserSession { Id = Guid.NewGuid(), UserId = userId, TenantId = tenantId, RefreshToken = "t1", ExpiresAt = DateTime.UtcNow.AddDays(1), IsRevoked = false, LastActiveAt = DateTime.UtcNow.AddMinutes(-10) };
        var active2 = new UserSession { Id = Guid.NewGuid(), UserId = userId, TenantId = tenantId, RefreshToken = "t2", ExpiresAt = DateTime.UtcNow.AddDays(2), IsRevoked = false, LastActiveAt = DateTime.UtcNow };
        var revoked = new UserSession { Id = Guid.NewGuid(), UserId = userId, TenantId = tenantId, RefreshToken = "t3", ExpiresAt = DateTime.UtcNow.AddDays(1), IsRevoked = true };
        var expired = new UserSession { Id = Guid.NewGuid(), UserId = userId, TenantId = tenantId, RefreshToken = "t4", ExpiresAt = DateTime.UtcNow.AddDays(-1), IsRevoked = false };
        var otherUser = new UserSession { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), TenantId = tenantId, RefreshToken = "t5", ExpiresAt = DateTime.UtcNow.AddDays(1), IsRevoked = false };

        context.Set<UserSession>().AddRange(active1, active2, revoked, expired, otherUser);
        await context.SaveChangesAsync();

        var sut = CreateSut();

        // Act
        var result = (await sut.GetActiveSessionsAsync(userId)).ToList();

        // Assert
        result.Should().HaveCount(2);
        result[0].Id.Should().Be(active2.Id); // OrderByDescending(LastActiveAt)
        result[1].Id.Should().Be(active1.Id);
    }

    [Fact]
    public async Task RevokeSessionAsync_WhenSessionExists_RevokesAndDeletesFromRedis()
    {
        // Arrange
        var context = _dbFactory.CreateContext();
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var session = new UserSession { Id = Guid.NewGuid(), UserId = userId, TenantId = tenantId, RefreshToken = "rev-rt", ExpiresAt = DateTime.UtcNow.AddDays(1), IsRevoked = false };
        context.Set<UserSession>().Add(session);
        await context.SaveChangesAsync();

        var sut = CreateSut();

        // Act
        var result = await sut.RevokeSessionAsync(session.Id, userId);

        // Assert
        result.Should().BeTrue();

        using (var checkContext = _dbFactory.CreateContext())
        {
            var dbSession = checkContext.Set<UserSession>().Find(session.Id);
            dbSession!.IsRevoked.Should().BeTrue();
        }

        _dbMock.Verify(d => d.KeyDeleteAsync(It.Is<RedisKey>(k => k.ToString().Contains(session.Id.ToString())), It.IsAny<CommandFlags>()), Times.Once);
        _dbMock.Verify(d => d.KeyDeleteAsync(It.Is<RedisKey>(k => k.ToString().Contains("rt:rev-rt")), It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task RevokeSessionAsync_WhenSessionDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.RevokeSessionAsync(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RevokeAllSessionsAsync_RevokesExpectedSessions()
    {
        // Arrange
        var context = _dbFactory.CreateContext();
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var sess1 = new UserSession { Id = Guid.NewGuid(), UserId = userId, TenantId = tenantId, RefreshToken = "r1", ExpiresAt = DateTime.UtcNow.AddDays(1), IsRevoked = false };
        var sess2 = new UserSession { Id = Guid.NewGuid(), UserId = userId, TenantId = tenantId, RefreshToken = "r2", ExpiresAt = DateTime.UtcNow.AddDays(1), IsRevoked = false };
        var sess3 = new UserSession { Id = Guid.NewGuid(), UserId = userId, TenantId = tenantId, RefreshToken = "r3", ExpiresAt = DateTime.UtcNow.AddDays(1), IsRevoked = false };

        context.Set<UserSession>().AddRange(sess1, sess2, sess3);
        await context.SaveChangesAsync();

        var sut = CreateSut();

        // Act - revoke all except sess2
        var count = await sut.RevokeAllSessionsAsync(userId, exceptSessionId: sess2.Id);

        // Assert
        count.Should().Be(2);

        using (var checkContext = _dbFactory.CreateContext())
        {
            checkContext.Set<UserSession>().Find(sess1.Id)!.IsRevoked.Should().BeTrue();
            checkContext.Set<UserSession>().Find(sess2.Id)!.IsRevoked.Should().BeFalse();
            checkContext.Set<UserSession>().Find(sess3.Id)!.IsRevoked.Should().BeTrue();
        }
    }

    [Fact]
    public async Task UpdateLastActiveAsync_UpdatesTimestamp()
    {
        // Arrange
        var context = _dbFactory.CreateContext();
        var session = new UserSession { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), TenantId = Guid.NewGuid(), RefreshToken = "tok", ExpiresAt = DateTime.UtcNow.AddDays(1), LastActiveAt = DateTime.UtcNow.AddDays(-1) };
        context.Set<UserSession>().Add(session);
        await context.SaveChangesAsync();

        var sut = CreateSut();

        // Act
        await sut.UpdateLastActiveAsync(session.Id);

        // Assert
        using (var checkContext = _dbFactory.CreateContext())
        {
            var dbSession = checkContext.Set<UserSession>().Find(session.Id);
            dbSession!.LastActiveAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }
    }

    [Fact]
    public async Task GetSessionByRefreshTokenAsync_RedisHit_ReturnsCachedSession()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var refreshToken = "rt-cached";
        var cachedSession = new UserSession
        {
            Id = sessionId,
            UserId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };

        _dbMock.Setup(d => d.StringGetAsync(It.Is<RedisKey>(k => k.ToString().Contains("rt:rt-cached")), It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisValue)sessionId.ToString());

        _dbMock.Setup(d => d.StringGetAsync(It.Is<RedisKey>(k => k.ToString().Contains(sessionId.ToString())), It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisValue)JsonSerializer.Serialize(cachedSession));

        var sut = CreateSut();

        // Act
        var result = await sut.GetSessionByRefreshTokenAsync(refreshToken);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(sessionId);
        result.RefreshToken.Should().Be(refreshToken);
    }

    [Fact]
    public async Task GetSessionByRefreshTokenAsync_RedisMiss_DbHit_CachesAndReturns()
    {
        // Arrange
        var context = _dbFactory.CreateContext();
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var refreshToken = "rt-db";
        var session = new UserSession { Id = Guid.NewGuid(), UserId = userId, TenantId = tenantId, RefreshToken = refreshToken, ExpiresAt = DateTime.UtcNow.AddDays(1), IsRevoked = false };
        context.Set<UserSession>().Add(session);
        await context.SaveChangesAsync();

        _dbMock.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        var sut = CreateSut();

        // Act
        var result = await sut.GetSessionByRefreshTokenAsync(refreshToken);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(session.Id);

        // Expiration exposes no instance TTL property, and renders as "EX <seconds>";
        // 30 minutes is 1800s.
        _dbMock.Verify(d => d.StringSetAsync(
            It.Is<RedisKey>(k => k.ToString().Contains(session.Id.ToString())),
            It.IsAny<RedisValue>(),
            It.Is<Expiration>(e => e.ToString() == "EX 1800"),
            It.IsAny<ValueCondition>(),
            It.IsAny<CommandFlags>()
        ), Times.Once);
    }

    [Theory]
    [InlineData("Mozilla/5.0 (iPad; CPU OS 14_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/14.0 Mobile/15E148 Safari/604.1", "tablet", "Safari", "iOS")]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36 Edg/91.0.864.59", "desktop", "Chrome", "Windows")]
    [InlineData("Mozilla/5.0 (Linux; Android 10; SM-A505F) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Mobile Safari/537.36 Firefox/89.0", "mobile", "Chrome", "Linux")]
    [InlineData(null, null, null, null)]
    public async Task CreateSessionAsync_UserAgentParsing_WorksAsExpected(string? userAgent, string? expectedDevice, string? expectedBrowser, string? expectedOS)
    {
        // Arrange
        var sut = CreateSut();
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        // Act
        var session = await sut.CreateSessionAsync(userId, tenantId, "rt-ua", "127.0.0.1", userAgent);

        // Assert
        session.DeviceType.Should().Be(expectedDevice);
        session.Browser.Should().Be(expectedBrowser);
        session.OperatingSystem.Should().Be(expectedOS);
    }
}
