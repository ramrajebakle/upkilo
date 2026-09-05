using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Upkilo.API.Controllers;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;
using Upkilo.Infrastructure.Services;
using Upkilo.Tests.Helpers;
using Xunit;

namespace Upkilo.Tests.Controllers;

/// <summary>
/// The knowledge base page and the assistant must read and write the SAME store.
///
/// This controller previously kept its entries in a pair of static ConcurrentDictionaries, so a
/// tenant could fill in the page called "Knowledge Base" and the assistant would still answer
/// "I don't have that information" — the prompt is built from the AIKnowledgeBases table, which
/// the controller never touched. The entries also vanished on restart and were invisible to any
/// other replica.
///
/// The load-bearing test here is <see cref="AddedEntry_IsVisibleToTheAssistantsContextBuilder"/>:
/// it writes through the controller and reads back through the real ChatbotContextBuilder, so it
/// fails if the two are ever pointed at different stores again.
/// </summary>
public class KnowledgeBaseControllerTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _otherTenantId = Guid.NewGuid();

    private KnowledgeBaseController BuildSut(AppDbContext db, Guid? tenantId = null)
    {
        var tenantProvider = new Mock<ITenantProvider>();
        tenantProvider.Setup(t => t.GetTenantId()).Returns(tenantId ?? _tenantId);
        tenantProvider.Setup(t => t.GetUserId()).Returns(Guid.NewGuid());

        return new KnowledgeBaseController(
            db, tenantProvider.Object, NullLogger<KnowledgeBaseController>.Instance);
    }

    private static KbEntryRequest Req(string title, string content, string? type = "faq") =>
        new() { Question = title, Answer = content, Category = type, Tags = new[] { "parking" } };

    // ── Persistence ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddedEntry_IsPersistedToTheDatabase()
    {
        var db = _factory.CreateContext();
        await BuildSut(db).AddEntry(Req("Do you offer parking?", "Yes, free behind the salon."));

        // Read through a SEPARATE context, which is what proves it left memory and hit the store.
        var saved = await _factory.CreateContext().AIKnowledgeBases
            .SingleAsync(k => k.TenantId == _tenantId);

        saved.Question.Should().Be("Do you offer parking?");
        saved.Answer.Should().Be("Yes, free behind the salon.");
        saved.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task AddedEntry_IsVisibleToTheAssistantsContextBuilder()
    {
        var db = _factory.CreateContext();
        await BuildSut(db).AddEntry(Req("Do you offer parking?", "Yes, free behind the salon."));

        // The real builder the chatbot uses, on its own context.
        var entitlements = new Mock<IEntitlementService>();
        entitlements
            .Setup(e => e.GetEffectiveEntitlementsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EntitlementSet());

        var builder = new ChatbotContextBuilder(_factory.CreateContext(), entitlements.Object);
        var context = await builder.BuildAsync(_tenantId, ChatAudience.PublicVisitor);

        context.KnowledgeBase.Should().Contain("free behind the salon",
            "an entry added on the knowledge base page must reach the assistant's prompt");
        context.HasTenantKnowledge.Should().BeTrue();
    }

    [Fact]
    public async Task Entries_SurviveANewControllerInstance()
    {
        await BuildSut(_factory.CreateContext()).AddEntry(Req("Q1", "A1"));

        // A different controller and context stands in for a restart or another replica; the old
        // static dictionary lost everything at this boundary.
        var result = await BuildSut(_factory.CreateContext()).GetEntries(type: null, search: null);

        var entries = ExtractEntries(result);
        entries.Should().ContainSingle().Which.Question.Should().Be("Q1");
    }

    // ── Tenant isolation ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Listing_ShowsOnlyTheCallersOwnEntries()
    {
        await BuildSut(_factory.CreateContext()).AddEntry(Req("Mine", "Mine answer"));
        await BuildSut(_factory.CreateContext(), _otherTenantId).AddEntry(Req("Theirs", "Their answer"));

        var result = await BuildSut(_factory.CreateContext()).GetEntries(null, null);

        var entries = ExtractEntries(result);
        entries.Should().ContainSingle().Which.Question.Should().Be("Mine");
    }

    [Fact]
    public async Task Update_OfAnotherTenantsEntry_Is404NotACrossTenantWrite()
    {
        var addResult = await BuildSut(_factory.CreateContext(), _otherTenantId)
            .AddEntry(Req("Theirs", "Their answer"));
        var theirId = ExtractEntry(addResult).Id;

        var result = await BuildSut(_factory.CreateContext())
            .UpdateEntry(theirId, Req("Hijacked", "Hijacked answer"));

        result.Should().BeOfType<NotFoundObjectResult>();

        // And the row is untouched.
        var row = await _factory.CreateContext().AIKnowledgeBases.SingleAsync(k => k.Id == theirId);
        row.Question.Should().Be("Theirs");
    }

    [Fact]
    public async Task Delete_OfAnotherTenantsEntry_Is404AndLeavesTheRow()
    {
        var addResult = await BuildSut(_factory.CreateContext(), _otherTenantId)
            .AddEntry(Req("Theirs", "Their answer"));
        var theirId = ExtractEntry(addResult).Id;

        var result = await BuildSut(_factory.CreateContext()).DeleteEntry(theirId);

        result.Should().BeOfType<NotFoundObjectResult>();
        (await _factory.CreateContext().AIKnowledgeBases.AnyAsync(k => k.Id == theirId))
            .Should().BeTrue();
    }

    // ── Round-tripping and validation ────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_ChangesThePersistedRow()
    {
        var addResult = await BuildSut(_factory.CreateContext()).AddEntry(Req("Old", "Old answer"));
        var id = ExtractEntry(addResult).Id;

        await BuildSut(_factory.CreateContext()).UpdateEntry(id, Req("New", "New answer"));

        var row = await _factory.CreateContext().AIKnowledgeBases.SingleAsync(k => k.Id == id);
        row.Question.Should().Be("New");
        row.Answer.Should().Be("New answer");
    }

    [Fact]
    public async Task Delete_RemovesTheRow()
    {
        var addResult = await BuildSut(_factory.CreateContext()).AddEntry(Req("Q", "A"));
        var id = ExtractEntry(addResult).Id;

        await BuildSut(_factory.CreateContext()).DeleteEntry(id);

        (await _factory.CreateContext().AIKnowledgeBases.AnyAsync(k => k.Id == id)).Should().BeFalse();
    }

    [Fact]
    public async Task Tags_SurviveTheRoundTripIncludingCommas()
    {
        var db = _factory.CreateContext();
        await BuildSut(db).AddEntry(new KbEntryRequest
        {
            Question = "Q",
            Answer = "A",
            Category = "faq",
            Tags = new[] { "parking, free", "hours" },
        });

        var entries = ExtractEntries(await BuildSut(_factory.CreateContext()).GetEntries(null, null));

        // Stored as JSON rather than a comma-join precisely so this tag is not split in two.
        entries.Single().Tags.Should().BeEquivalentTo(new[] { "parking, free", "hours" });
    }

    [Fact]
    public async Task OverlongEntry_IsRejectedBeforeItReachesThePrompt()
    {
        var result = await BuildSut(_factory.CreateContext())
            .AddEntry(Req("Q", new string('x', 2001)));

        result.Should().BeOfType<BadRequestObjectResult>();
        (await _factory.CreateContext().AIKnowledgeBases.AnyAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task Stats_ReportLastTrainedFromTheNewestEntry()
    {
        await BuildSut(_factory.CreateContext()).AddEntry(Req("Q", "A"));

        var stats = ExtractStats(await BuildSut(_factory.CreateContext()).GetStats());

        stats.TotalEntries.Should().Be(1);
        // Previously read from a static dictionary that emptied on restart, so it blanked itself
        // for no reason the user could see.
        stats.LastTrainedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Stats_OnAnEmptyKnowledgeBase_ReportNothingRatherThanADefaultDate()
    {
        var stats = ExtractStats(await BuildSut(_factory.CreateContext()).GetStats());

        stats.TotalEntries.Should().Be(0);
        stats.LastTrainedAt.Should().BeNull();
    }

    // ── Result unwrapping ────────────────────────────────────────────────────────────────────

    private static object Payload(IActionResult result) => result switch
    {
        OkObjectResult ok => ok.Value!,
        CreatedAtActionResult created => created.Value!,
        AcceptedResult accepted => accepted.Value!,
        _ => throw new InvalidOperationException($"Unexpected result: {result.GetType().Name}"),
    };

    /// <summary>Reads ApiResponse&lt;T&gt;.Data without binding to its concrete generic type.</summary>
    private static T Data<T>(IActionResult result)
    {
        var payload = Payload(result);
        var prop = payload.GetType().GetProperty("Data")
                   ?? throw new InvalidOperationException("No Data property on the response envelope");
        return (T)prop.GetValue(payload)!;
    }

    private static List<KbEntry> ExtractEntries(IActionResult result) =>
        Data<IEnumerable<KbEntry>>(result).ToList();

    private static KbEntry ExtractEntry(IActionResult result) => Data<KbEntry>(result);

    private static KbStats ExtractStats(IActionResult result) => Data<KbStats>(result);

    public void Dispose() => _factory.Dispose();
}
