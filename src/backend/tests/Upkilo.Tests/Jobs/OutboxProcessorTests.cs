using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;
using Upkilo.Infrastructure.Jobs;
using Upkilo.Tests.Helpers;
using Xunit;

namespace Upkilo.Tests.Jobs;

/// <summary>
/// Guards the outbox processor against the failure mode that took production down:
/// a dispatch failure wrote ex.ToString() into OutboxMessage.Error, which is
/// character varying(500). PostgreSQL rejected the oversized value with 22001, the
/// final SaveChangesAsync threw, and the whole Hangfire job failed. Because the failed
/// write was the one that would have recorded the message's retry state, the message
/// stayed pending and the next run 30 seconds later failed on it again — permanently.
/// The accumulated Failed jobs pushed the "hangfire" health check to Degraded, which
/// blocked every deployment.
///
/// These tests run on SQLite, which does NOT enforce varchar limits — that is exactly
/// why the original bug reached production. So they assert the invariant that protects
/// the Postgres column (the written length) rather than relying on the provider to fail.
/// </summary>
public class OutboxProcessorTests : IDisposable
{
    /// <summary>Mirrors OutboxMessage.Error — "character varying(500)".</summary>
    private const int ErrorColumnLength = 500;

    private readonly TestDbContextFactory _dbFactory;

    public OutboxProcessorTests() => _dbFactory = new TestDbContextFactory();
    public void Dispose() => _dbFactory.Dispose();

    private static OutboxProcessor Sut(AppDbContext ctx, IWebhookService webhooks) =>
        new(ctx, webhooks, NullLogger<OutboxProcessor>.Instance);

    /// <summary>A webhook service whose dispatch always throws with a huge message.</summary>
    private static IWebhookService FailingWebhooks(int messageLength = 5000)
    {
        var mock = new Mock<IWebhookService>();
        mock.Setup(w => w.DispatchEventAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<object>()))
            .ThrowsAsync(new InvalidOperationException(new string('x', messageLength)));
        return mock.Object;
    }

    private static async Task<OutboxMessage> SeedMessageAsync(
        AppDbContext ctx, string eventType = "Webhook.BookingCreated", int retryCount = 0)
    {
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            EventType = eventType,
            Payload = "{}",
            RetryCount = retryCount
        };
        ctx.OutboxMessages.Add(message);
        await ctx.SaveChangesAsync();
        return message;
    }

    // ── The regression: an oversized error must never reach the column ────

    [Fact]
    public async Task FailedDispatch_TruncatesErrorToColumnLength()
    {
        var ctx = _dbFactory.CreateContext();
        var message = await SeedMessageAsync(ctx);

        await Sut(ctx, FailingWebhooks()).ProcessPendingMessagesAsync();

        var saved = await ctx.OutboxMessages.AsNoTracking().SingleAsync(m => m.Id == message.Id);
        saved.Error.Should().NotBeNullOrEmpty("the failure reason must still be recorded");
        saved.Error!.Length.Should().BeLessThanOrEqualTo(
            ErrorColumnLength,
            "a longer value is rejected by PostgreSQL with 22001 and fails the whole job");
    }

    [Fact]
    public async Task FailedDispatch_DoesNotThrowOutOfTheJob()
    {
        var ctx = _dbFactory.CreateContext();
        await SeedMessageAsync(ctx);

        var act = async () => await Sut(ctx, FailingWebhooks()).ProcessPendingMessagesAsync();

        await act.Should().NotThrowAsync(
            "an unhandled exception marks the Hangfire job Failed, and 2,880 runs a day " +
            "then hold the hangfire health check at Degraded");
    }

    /// <summary>
    /// The heart of the outage: the write recording retry state was the write that failed,
    /// so the message never advanced and every subsequent run failed on it identically.
    /// </summary>
    [Fact]
    public async Task FailedDispatch_PersistsRetryState_SoTheMessageCannotPoisonEveryRun()
    {
        var ctx = _dbFactory.CreateContext();
        var message = await SeedMessageAsync(ctx);

        await Sut(ctx, FailingWebhooks()).ProcessPendingMessagesAsync();

        var saved = await ctx.OutboxMessages.AsNoTracking().SingleAsync(m => m.Id == message.Id);
        saved.RetryCount.Should().Be(1, "the attempt must be recorded or the message retries forever");
        saved.NextRetryAt.Should().NotBeNull("backoff must be scheduled");
        saved.IsProcessed.Should().BeFalse();
    }

    [Fact]
    public async Task MessageWithExhaustedRetries_IsDeadLettered()
    {
        var ctx = _dbFactory.CreateContext();
        // BackoffSchedule has 3 slots, so RetryCount == 3 is the last pass before the DLQ.
        var message = await SeedMessageAsync(ctx, retryCount: 3);

        await Sut(ctx, FailingWebhooks()).ProcessPendingMessagesAsync();

        var saved = await ctx.OutboxMessages.AsNoTracking().SingleAsync(m => m.Id == message.Id);
        saved.IsDeadLetter.Should().BeTrue("a poison message must leave the retry rotation");
        saved.DeadLetteredAt.Should().NotBeNull();
    }

    // ── The happy path still works ────────────────────────────────────────

    [Fact]
    public async Task SuccessfulDispatch_MarksMessageProcessed()
    {
        var ctx = _dbFactory.CreateContext();
        var message = await SeedMessageAsync(ctx);

        var webhooks = new Mock<IWebhookService>();
        webhooks.Setup(w => w.DispatchEventAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<object>()))
                .Returns(Task.CompletedTask);

        await Sut(ctx, webhooks.Object).ProcessPendingMessagesAsync();

        var saved = await ctx.OutboxMessages.AsNoTracking().SingleAsync(m => m.Id == message.Id);
        saved.IsProcessed.Should().BeTrue();
        saved.ProcessedAt.Should().NotBeNull();
        saved.Error.Should().BeNull();
    }

    [Fact]
    public async Task NonWebhookMessage_IsMarkedProcessedWithoutDispatch()
    {
        var ctx = _dbFactory.CreateContext();
        var webhooks = new Mock<IWebhookService>();
        var message = await SeedMessageAsync(ctx, eventType: "Booking.Created");

        await Sut(ctx, webhooks.Object).ProcessPendingMessagesAsync();

        webhooks.Verify(
            w => w.DispatchEventAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<object>()),
            Times.Never,
            "only Webhook.* messages are dispatched to webhook endpoints");
        var saved = await ctx.OutboxMessages.AsNoTracking().SingleAsync(m => m.Id == message.Id);
        saved.IsProcessed.Should().BeTrue();
    }

    [Fact]
    public async Task AlreadyProcessedAndDeadLetteredMessages_AreNotPickedUp()
    {
        var ctx = _dbFactory.CreateContext();
        var webhooks = new Mock<IWebhookService>();

        var processed = await SeedMessageAsync(ctx);
        processed.IsProcessed = true;
        var deadLettered = await SeedMessageAsync(ctx);
        deadLettered.IsDeadLetter = true;
        await ctx.SaveChangesAsync();

        await Sut(ctx, webhooks.Object).ProcessPendingMessagesAsync();

        webhooks.Verify(
            w => w.DispatchEventAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<object>()),
            Times.Never);
    }
}
