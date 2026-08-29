using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StackExchange.Redis;
using Upkilo.API.Jobs;
using Upkilo.Core.Interfaces;
using Upkilo.Tests.Helpers;
using Xunit;

namespace Upkilo.Tests.Jobs;

/// <summary>
/// BookingReminderJob takes a Redis lock so two replicas cannot remind the same booking
/// twice. That lock call used to be unguarded, so whenever Redis was briefly unreachable
/// the job threw and Hangfire marked the run Failed — 96 times a day. Hangfire keeps failed
/// jobs forever, so one short Redis incident permanently pinned the "hangfire" health check
/// at Degraded long after Redis recovered, and the production deploy gate refused to ship
/// while anything was not Healthy.
///
/// A Redis outage must therefore SKIP the run: never throw, and never run unlocked (which
/// would send duplicate reminders to real customers).
/// </summary>
public class BookingReminderJobRedisTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory;

    public BookingReminderJobRedisTests() => _dbFactory = new TestDbContextFactory();
    public void Dispose() => _dbFactory.Dispose();

    private BookingReminderJob SutWithRedisThatThrows(Exception toThrow)
    {
        var db = new Mock<IDatabase>();
        db.Setup(d => d.LockTakeAsync(
                It.IsAny<RedisKey>(), It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan>(), It.IsAny<CommandFlags>()))
          .ThrowsAsync(toThrow);

        var redis = new Mock<IConnectionMultiplexer>();
        redis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(db.Object);

        return new BookingReminderJob(
            _dbFactory.CreateContext(),
            Mock.Of<IEmailService>(),
            Mock.Of<ISmsService>(),
            Mock.Of<IWhatsAppService>(),
            Mock.Of<ITimezoneService>(),
            NullLogger<BookingReminderJob>.Instance,
            redis.Object);
    }

    [Fact]
    public async Task RedisConnectionFailure_SkipsRunInsteadOfFailingTheJob()
    {
        var sut = SutWithRedisThatThrows(
            new RedisConnectionException(ConnectionFailureType.UnableToConnect, "no connection available"));

        var act = async () => await sut.ExecuteAsync();

        await act.Should().NotThrowAsync(
            "an unhandled Redis error marks the Hangfire job Failed, and those records never expire");
    }

    /// <summary>
    /// RedisTimeoutException derives from TimeoutException rather than RedisException, so a
    /// catch written only for RedisException would still let this one fail the job.
    /// </summary>
    [Fact]
    public async Task RedisTimeout_SkipsRunInsteadOfFailingTheJob()
    {
        var sut = SutWithRedisThatThrows(
            new RedisTimeoutException("timed out in the backlog", CommandStatus.WaitingToBeSent));

        var act = async () => await sut.ExecuteAsync();

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task LockHeldByAnotherWorker_SkipsQuietly()
    {
        var db = new Mock<IDatabase>();
        db.Setup(d => d.LockTakeAsync(
                It.IsAny<RedisKey>(), It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan>(), It.IsAny<CommandFlags>()))
          .ReturnsAsync(false);

        var redis = new Mock<IConnectionMultiplexer>();
        redis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(db.Object);

        var sut = new BookingReminderJob(
            _dbFactory.CreateContext(),
            Mock.Of<IEmailService>(), Mock.Of<ISmsService>(), Mock.Of<IWhatsAppService>(),
            Mock.Of<ITimezoneService>(), NullLogger<BookingReminderJob>.Instance, redis.Object);

        var act = async () => await sut.ExecuteAsync();

        await act.Should().NotThrowAsync();
        // The lock was never acquired, so it must not be released either.
        db.Verify(d => d.LockReleaseAsync(
            It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()), Times.Never);
    }
}
