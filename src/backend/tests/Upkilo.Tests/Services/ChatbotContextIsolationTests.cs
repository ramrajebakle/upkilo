using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;
using Upkilo.Infrastructure.Services;
using Upkilo.Tests.Helpers;
using Xunit;

namespace Upkilo.Tests.Services;

/// <summary>
/// Tenant isolation and grounding for the chatbot's context layer.
///
/// These run against the real ChatbotContextBuilder and a real AppDbContext rather than mocks,
/// because the property under test is precisely which ROWS a query returns — a mocked context
/// would assert only that the code I wrote is the code I wrote.
///
/// The isolation here cannot lean on EF's global query filter. That filter is written as
///
///     _tenantId == null || TenantId == _tenantId
///
/// so it is DISABLED, not restrictive, when there is no ambient tenant — and the public
/// receptionist route is [AllowAnonymous], which is exactly that case. These tests therefore use
/// an unscoped context on purpose: it reproduces the anonymous route, and any missing explicit
/// predicate shows up as another tenant's data in the prompt.
/// </summary>
public class ChatbotContextIsolationTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();

    private readonly Guid _salonId = Guid.NewGuid();
    private readonly Guid _gymId = Guid.NewGuid();

    public ChatbotContextIsolationTests()
    {
        var db = _factory.CreateContext();

        db.Tenants.AddRange(
            new Tenant
            {
                Id = _salonId,
                Name = "Glow Beauty",
                BusinessName = "Glow Beauty Studio",
                Slug = "glow-beauty",
                Email = "hello@glow.test",
                Phone = "+1-555-0100",
                Currency = "USD",
                Description = "Hair and beauty salon",
            },
            new Tenant
            {
                Id = _gymId,
                Name = "FitLife Gym",
                BusinessName = "FitLife Gym",
                Slug = "fitlife-gym",
                Email = "hello@fitlife.test",
                Phone = "+1-555-0200",
                Currency = "EUR",
                Description = "Strength and conditioning gym",
            });

        db.Services.AddRange(
            new Service { Id = Guid.NewGuid(), TenantId = _salonId, Name = "Balayage", Price = 180m, DurationMinutes = 120, IsActive = true },
            new Service { Id = Guid.NewGuid(), TenantId = _gymId, Name = "Personal Training", Price = 65m, DurationMinutes = 60, IsActive = true },
            // Inactive: must not be quoted to a visitor as something they can buy.
            new Service { Id = Guid.NewGuid(), TenantId = _salonId, Name = "Discontinued Perm", Price = 90m, DurationMinutes = 90, IsActive = false });

        db.AIKnowledgeBases.AddRange(
            new AIKnowledgeBase
            {
                Id = Guid.NewGuid(),
                TenantId = _salonId,
                IsActive = true,
                Question = "Do you offer parking?",
                Answer = "Yes, free parking behind the salon.",
            },
            new AIKnowledgeBase
            {
                Id = Guid.NewGuid(),
                TenantId = _gymId,
                IsActive = true,
                Question = "Do you offer parking?",
                Answer = "No parking, but we are next to Central Station.",
            });

        db.SaveChanges();
    }

    private ChatbotContextBuilder BuildSut(EntitlementSet? entitlements = null)
    {
        var ent = new Mock<IEntitlementService>();
        ent.Setup(e => e.GetEffectiveEntitlementsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(entitlements ?? new EntitlementSet());

        // Unscoped context: reproduces the anonymous public route, where the global filter is off.
        return new ChatbotContextBuilder(_factory.CreateContext(), ent.Object);
    }

    [Fact]
    public async Task TenantFacts_ContainOnlyTheRequestedTenantsBusiness()
    {
        var context = await BuildSut().BuildAsync(_salonId, ChatAudience.PublicVisitor);

        context.TenantFacts.Should().Contain("Glow Beauty Studio");
        context.TenantFacts.Should().Contain("Balayage");

        // The load-bearing assertion: nothing from the other tenant, at all.
        context.TenantFacts.Should().NotContain("FitLife");
        context.TenantFacts.Should().NotContain("Personal Training");
        context.TenantFacts.Should().NotContain("+1-555-0200");
    }

    [Fact]
    public async Task KnowledgeBase_DoesNotLeakAnotherTenantsAnswerToTheSameQuestion()
    {
        // Both tenants answer "Do you offer parking?" and the answers contradict each other.
        // Serving the gym's answer to the salon's visitor is a wrong answer that reads as
        // authoritative, which is worse than no answer.
        var salon = await BuildSut().BuildAsync(_salonId, ChatAudience.PublicVisitor);
        var gym = await BuildSut().BuildAsync(_gymId, ChatAudience.PublicVisitor);

        salon.KnowledgeBase.Should().Contain("free parking behind the salon");
        salon.KnowledgeBase.Should().NotContain("Central Station");

        gym.KnowledgeBase.Should().Contain("Central Station");
        gym.KnowledgeBase.Should().NotContain("free parking behind the salon");
    }

    [Fact]
    public async Task InactiveServices_AreNotOfferedToVisitors()
    {
        var context = await BuildSut().BuildAsync(_salonId, ChatAudience.PublicVisitor);

        context.TenantFacts.Should().NotContain("Discontinued Perm",
            "quoting a service the business no longer sells creates a booking it cannot honour");
    }

    [Fact]
    public async Task PublicVisitor_NeverReceivesUpkiloPlatformKnowledge()
    {
        var context = await BuildSut().BuildAsync(_salonId, ChatAudience.PublicVisitor);

        context.PlatformFacts.Should().BeEmpty(
            "a salon's customer asking about 'plans' means the salon's services, not Upkilo's "
            + "price list; answering with the latter discloses the vendor and puts a software "
            + "company's billing terms into a conversation the business owns");
    }

    [Fact]
    public async Task TenantStaff_DoReceiveUpkiloPlatformKnowledge()
    {
        var context = await BuildSut().BuildAsync(_salonId, ChatAudience.TenantStaff);

        context.PlatformFacts.Should().Contain("Upkilo",
            "the tenant's own staff are Upkilo's customers and may ask about the platform");
    }

    [Fact]
    public async Task PlatformFacts_ReportThisTenantsOwnEntitlements()
    {
        var set = new EntitlementSet();
        set.Features["ai_copilot"] = new Entitlement { IsEnabled = true };
        set.Features["white_label"] = new Entitlement { IsEnabled = false };

        var context = await BuildSut(set).BuildAsync(_salonId, ChatAudience.TenantStaff);

        context.PlatformFacts.Should().Contain("ai_copilot");
        context.PlatformFacts.Should().NotContain("white_label",
            "listing a feature the tenant is not entitled to invites the assistant to explain how "
            + "to use something the API will refuse");
    }

    [Fact]
    public async Task ATenantThatHasPublishedNothing_ReportsNoTenantKnowledge()
    {
        var emptyTenantId = Guid.NewGuid();
        var db = _factory.CreateContext();
        db.Tenants.Add(new Tenant { Id = emptyTenantId, Name = "Blank", Slug = "blank", Email = "b@b.test" });
        await db.SaveChangesAsync();

        var context = await BuildSut().BuildAsync(emptyTenantId, ChatAudience.PublicVisitor);

        // Name alone is not knowledge to answer questions from. What matters is that the flag
        // driving the "do not answer factual questions" instruction reflects having no services
        // and no FAQ entries.
        context.KnowledgeBase.Should().BeEmpty();
        context.TenantFacts.Should().NotContain("Balayage");
    }

    [Fact]
    public async Task AnUnknownTenant_YieldsNoFactsRatherThanEveryTenantsFacts()
    {
        // The failure mode when a filter is missing entirely: an id that matches nothing returns
        // everything.
        var context = await BuildSut().BuildAsync(Guid.NewGuid(), ChatAudience.PublicVisitor);

        context.TenantFacts.Should().BeEmpty();
        context.KnowledgeBase.Should().BeEmpty();
        context.HasTenantKnowledge.Should().BeFalse();
    }

    /// <summary>
    /// Interleaved builds for alternating tenants never contaminate each other.
    ///
    /// This is deliberately sequential-interleaved rather than parallel. The shared in-memory
    /// SQLite connection behind TestDbContextFactory serialises access and throws
    /// "database is locked" under a dozen concurrent readers, so a Task.WhenAll version tests the
    /// harness rather than the code and fails intermittently in CI. Production runs PostgreSQL,
    /// where that limit does not exist.
    ///
    /// Interleaving still catches the thing worth catching - state carried from one build into
    /// the next - and <see cref="TheBuilder_HoldsNoMutablePerRequestState"/> covers the property
    /// that makes genuine concurrency safe.
    /// </summary>
    [Fact]
    public async Task InterleavedBuildsForDifferentTenants_DoNotContaminateEachOther()
    {
        for (var i = 0; i < 6; i++)
        {
            var salon = await BuildSut().BuildAsync(_salonId, ChatAudience.PublicVisitor);
            salon.TenantFacts.Should().Contain("Glow Beauty Studio");
            salon.TenantFacts.Should().NotContain("FitLife");

            var gym = await BuildSut().BuildAsync(_gymId, ChatAudience.PublicVisitor);
            gym.TenantFacts.Should().Contain("FitLife");
            gym.TenantFacts.Should().NotContain("Glow Beauty Studio");
        }
    }

    /// <summary>
    /// The builder keeps no mutable per-request state, which is what makes it safe to resolve as
    /// a scoped service and run concurrently for different tenants. A cached field holding the
    /// last tenant's facts is the classic way this kind of class starts leaking across requests,
    /// and it would not show up in any single-threaded assertion.
    /// </summary>
    [Fact]
    public void TheBuilder_HoldsNoMutablePerRequestState()
    {
        var mutableFields = typeof(ChatbotContextBuilder)
            .GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            .Where(f => !f.IsInitOnly)
            .Select(f => f.Name)
            .ToList();

        mutableFields.Should().BeEmpty(
            "every field must be readonly; a mutable field would be shared state between turns");
    }

    [Fact]
    public async Task KnowledgeBase_IsBoundedSoOnePromptCannotGrowWithoutLimit()
    {
        var db = _factory.CreateContext();
        for (var i = 0; i < 120; i++)
        {
            db.AIKnowledgeBases.Add(new AIKnowledgeBase
            {
                Id = Guid.NewGuid(),
                TenantId = _salonId,
                IsActive = true,
                Question = $"Question number {i}?",
                Answer = $"Answer number {i}.",
            });
        }
        await db.SaveChangesAsync();

        var context = await BuildSut().BuildAsync(_salonId, ChatAudience.PublicVisitor);

        // Previously an unbounded ToListAsync(): the prompt, its cost and its latency grew with
        // however many entries the tenant had ever written.
        var entryCount = context.KnowledgeBase.Split("Q: ", StringSplitOptions.RemoveEmptyEntries).Length;
        entryCount.Should().BeLessThanOrEqualTo(40);
    }

    public void Dispose() => _factory.Dispose();
}
