using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;
using Upkilo.Infrastructure.Services;
using Upkilo.Tests.Helpers;
using Xunit;

namespace Upkilo.Tests.Services;

/// <summary>
/// Resolution tests for the entitlement engine.
///
/// The suite these replace only ever asserted the NEGATIVE cases — unknown key returns false,
/// no mappings returns false — which is why the catalogue mismatch survived: those assertions
/// pass just as happily when the entire key vocabulary is wrong and every tenant is denied.
/// Every test here therefore pins a POSITIVE expectation as well: that a customer who is
/// entitled to something actually gets it.
/// </summary>
public class EntitlementServiceTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory;
    private readonly AppDbContext _context;
    private readonly IDistributedCache _cache;
    private readonly EntitlementService _sut;

    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _planId = Guid.NewGuid();

    public EntitlementServiceTests()
    {
        _dbFactory = new TestDbContextFactory();
        _context = _dbFactory.CreateContext();
        _cache = MockFactory.CreateMemoryCache();
        _sut = MockFactory.CreateEntitlementService(_context, _cache);
    }

    public void Dispose() => _dbFactory.Dispose();

    /// <summary>
    /// A plan shaped like the real Growth tier: AI insights and API on, advanced security off,
    /// 25 staff, unlimited clients.
    /// </summary>
    private void SeedPlanAndSubscription(
        SubscriptionStatus status = SubscriptionStatus.Active,
        int extraStaff = 0)
    {
        var plan = new PricingPlan { Id = _planId, Name = "Growth", IsActive = true };
        _context.PricingPlans.Add(plan);

        void Map(string key, bool enabled, int? limit = null)
        {
            var feature = new PricingFeature { Key = key, Name = key, Type = FeatureType.Boolean };
            _context.PricingFeatures.Add(feature);
            _context.PlanFeatureMappings.Add(new PlanFeatureMapping
            {
                PricingPlan = plan,
                PricingFeature = feature,
                IsEnabled = enabled,
                NumericLimit = limit,
            });
        }

        Map(FeatureKeys.AiInsights, true);
        Map(FeatureKeys.AiCopilot, true);
        Map(FeatureKeys.ApiAccess, true);
        Map(FeatureKeys.WhiteLabel, true);
        Map(FeatureKeys.AdvancedSecurity, false);
        Map(FeatureKeys.MaxStaff, true, 25);
        Map(FeatureKeys.MaxClients, true, null);   // null on an enabled mapping = unlimited
        Map(FeatureKeys.AiActions, true, 10000);

        _context.Subscriptions.Add(new Subscription
        {
            TenantId = _tenantId,
            PricingPlanId = _planId,
            PricingPlan = plan,
            Status = status,
            CurrentPeriodStart = DateTime.UtcNow.AddDays(-5),
            CurrentPeriodEnd = DateTime.UtcNow.AddDays(25),
            ExtraStaffCount = extraStaff,
        });

        _context.SaveChanges();
    }

    private void AddOverride(
        string key, bool enabled, int? limit = null,
        DateTime? startsAt = null, DateTime? expiresAt = null)
    {
        _context.Set<TenantFeatureOverride>().Add(new TenantFeatureOverride
        {
            TenantId = _tenantId,
            FeatureKey = key,
            IsEnabled = enabled,
            NumericLimit = limit,
            StartsAt = startsAt,
            ExpiresAt = expiresAt,
            Reason = "test",
        });
        _context.SaveChanges();
    }

    // ── Plan resolution ───────────────────────────────────────────────────────

    [Fact]
    public async Task ActivePlan_GrantsIncludedFeature()
    {
        SeedPlanAndSubscription();

        // The regression that matters most: before the catalogue was unified this returned
        // false for every tenant on every plan.
        (await _sut.HasFeatureAsync(_tenantId, FeatureKeys.AiInsights)).Should().BeTrue();
    }

    [Fact]
    public async Task ActivePlan_DeniesExcludedFeature()
    {
        SeedPlanAndSubscription();

        (await _sut.HasFeatureAsync(_tenantId, FeatureKeys.AdvancedSecurity)).Should().BeFalse();

        var set = await _sut.GetEffectiveEntitlementsAsync(_tenantId);
        set.Features[FeatureKeys.AdvancedSecurity].Source.Should().Be(EntitlementSource.PlanExcluded);
    }

    [Fact]
    public async Task EveryCatalogueKey_IsResolved_EvenWhenPlanDoesNotMentionIt()
    {
        SeedPlanAndSubscription();

        var set = await _sut.GetEffectiveEntitlementsAsync(_tenantId);

        // Callers must never have to distinguish "absent" from "disabled".
        set.Features.Keys.Should().BeEquivalentTo(FeatureKeys.All);
        set.Features[FeatureKeys.MarketingAutomation].IsEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task NoSubscription_DeniesEverything()
    {
        var set = await _sut.GetEffectiveEntitlementsAsync(_tenantId);

        set.Features.Values.Should().OnlyContain(e => !e.IsEnabled);
        set.Features[FeatureKeys.AiCopilot].Source.Should().Be(EntitlementSource.NoSubscription);
    }

    // ── Subscription lifecycle gate ───────────────────────────────────────────

    [Theory]
    [InlineData(SubscriptionStatus.Active)]
    [InlineData(SubscriptionStatus.Trialing)]
    [InlineData(SubscriptionStatus.Trial)]
    [InlineData(SubscriptionStatus.PastDue)]   // inside the 14-day dunning grace
    public async Task EntitledStatuses_KeepPlanFeatures(SubscriptionStatus status)
    {
        SeedPlanAndSubscription(status);

        (await _sut.HasFeatureAsync(_tenantId, FeatureKeys.AiInsights)).Should().BeTrue();
    }

    [Theory]
    [InlineData(SubscriptionStatus.Cancelled)]
    [InlineData(SubscriptionStatus.Expired)]
    [InlineData(SubscriptionStatus.Suspended)]
    [InlineData(SubscriptionStatus.Paused)]
    public async Task UnentitledStatuses_RevokePlanFeatures(SubscriptionStatus status)
    {
        SeedPlanAndSubscription(status);

        // The revenue leak this engine was built to close: resolution used to consult the plan
        // and never the status, so a cancelled tenant kept every paid feature indefinitely.
        (await _sut.HasFeatureAsync(_tenantId, FeatureKeys.AiInsights)).Should().BeFalse();

        var set = await _sut.GetEffectiveEntitlementsAsync(_tenantId);
        set.IsServiceEntitled.Should().BeFalse();
        set.Features[FeatureKeys.AiInsights].Source.Should().Be(EntitlementSource.SubscriptionInactive);
        // The plan still says yes — that is what makes the override inspector legible.
        set.Features[FeatureKeys.AiInsights].PlanValue.Should().BeTrue();
    }

    // ── Overrides ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Override_GrantsFeatureThePlanExcludes()
    {
        SeedPlanAndSubscription();
        AddOverride(FeatureKeys.AdvancedSecurity, enabled: true);

        (await _sut.HasFeatureAsync(_tenantId, FeatureKeys.AdvancedSecurity)).Should().BeTrue();

        var set = await _sut.GetEffectiveEntitlementsAsync(_tenantId);
        set.Features[FeatureKeys.AdvancedSecurity].Source.Should().Be(EntitlementSource.Override);
        set.Features[FeatureKeys.AdvancedSecurity].PlanValue.Should().BeFalse();
    }

    [Fact]
    public async Task Override_RevokesFeatureThePlanIncludes()
    {
        SeedPlanAndSubscription();
        AddOverride(FeatureKeys.ApiAccess, enabled: false);

        (await _sut.HasFeatureAsync(_tenantId, FeatureKeys.ApiAccess)).Should().BeFalse();
    }

    [Fact]
    public async Task Override_DoesNotLeakToAnotherTenant()
    {
        SeedPlanAndSubscription();
        AddOverride(FeatureKeys.AdvancedSecurity, enabled: true);

        var otherTenant = Guid.NewGuid();
        _context.Subscriptions.Add(new Subscription
        {
            TenantId = otherTenant,
            PricingPlanId = _planId,
            Status = SubscriptionStatus.Active,
        });
        _context.SaveChanges();

        // Same plan, different tenant — a customer-specific deal must not reprice the plan.
        (await _sut.HasFeatureAsync(otherTenant, FeatureKeys.AdvancedSecurity)).Should().BeFalse();
    }

    [Fact]
    public async Task ExpiredOverride_FallsBackToPlan()
    {
        SeedPlanAndSubscription();
        AddOverride(FeatureKeys.AdvancedSecurity, enabled: true, expiresAt: DateTime.UtcNow.AddMinutes(-1));

        // Nothing sweeps the table, so expiry has to be evaluated at read time or an expired
        // grant would stay live forever.
        (await _sut.HasFeatureAsync(_tenantId, FeatureKeys.AdvancedSecurity)).Should().BeFalse();
    }

    [Fact]
    public async Task ScheduledOverride_IsInertUntilItStarts()
    {
        SeedPlanAndSubscription();
        AddOverride(FeatureKeys.AdvancedSecurity, enabled: true, startsAt: DateTime.UtcNow.AddDays(1));

        (await _sut.HasFeatureAsync(_tenantId, FeatureKeys.AdvancedSecurity)).Should().BeFalse();
    }

    [Fact]
    public async Task Override_OutranksInactiveSubscription()
    {
        SeedPlanAndSubscription(SubscriptionStatus.Cancelled);
        AddOverride(FeatureKeys.AiCopilot, enabled: true, expiresAt: DateTime.UtcNow.AddDays(7));

        // Deliberate: a goodwill grant during a billing dispute has to survive the status gate,
        // which is exactly why ExpiresAt is the primary control on grants.
        (await _sut.HasFeatureAsync(_tenantId, FeatureKeys.AiCopilot)).Should().BeTrue();
    }

    // ── Limits ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task NumericLimit_ComesFromPlan()
        => await AssertLimit(FeatureKeys.MaxStaff, 25);

    [Fact]
    public async Task EnabledMappingWithNullLimit_MeansUnlimited()
        => await AssertLimit(FeatureKeys.MaxClients, EntitlementLimits.Unlimited);

    [Fact]
    public async Task ExpansionSeats_AddToTheEffectiveLimit()
    {
        SeedPlanAndSubscription(extraStaff: 5);

        (await _sut.GetLimitAsync(_tenantId, FeatureKeys.MaxStaff)).Should().Be(30);
    }

    [Fact]
    public async Task ExpansionSeats_DoNotInflateAnUnlimitedFeature()
    {
        SeedPlanAndSubscription(extraStaff: 5);

        (await _sut.GetLimitAsync(_tenantId, FeatureKeys.MaxClients))
            .Should().Be(EntitlementLimits.Unlimited);
    }

    [Fact]
    public async Task OverrideLimit_ReplacesThePlanLimit()
    {
        SeedPlanAndSubscription();
        AddOverride(FeatureKeys.MaxStaff, enabled: true, limit: 100);

        (await _sut.GetLimitAsync(_tenantId, FeatureKeys.MaxStaff)).Should().Be(100);
    }

    [Fact]
    public async Task OverrideWithNullLimit_InheritsThePlanLimit()
    {
        SeedPlanAndSubscription();
        AddOverride(FeatureKeys.MaxStaff, enabled: true, limit: null);

        (await _sut.GetLimitAsync(_tenantId, FeatureKeys.MaxStaff)).Should().Be(25);
    }

    [Fact]
    public async Task LimitOfDeniedFeature_IsZero()
    {
        SeedPlanAndSubscription(SubscriptionStatus.Cancelled);

        (await _sut.GetLimitAsync(_tenantId, FeatureKeys.MaxStaff)).Should().Be(EntitlementLimits.None);
    }

    private async Task AssertLimit(string key, int expected)
    {
        SeedPlanAndSubscription();
        (await _sut.GetLimitAsync(_tenantId, key)).Should().Be(expected);
    }

    // ── Seat limit guard ──────────────────────────────────────────────────────

    [Fact]
    public async Task SeatGuard_AllowsCreateBelowTheLimit()
    {
        SeedPlanAndSubscription();

        var result = await Upkilo.API.Helpers.SeatLimitGuard.CheckAsync(
            _sut, _tenantId, FeatureKeys.MaxStaff, () => Task.FromResult(24), "Staff");

        result.Should().BeNull("24 of 25 seats used means the 25th create must succeed");
    }

    [Fact]
    public async Task SeatGuard_RefusesCreateAtTheLimit()
    {
        SeedPlanAndSubscription();

        var result = await Upkilo.API.Helpers.SeatLimitGuard.CheckAsync(
            _sut, _tenantId, FeatureKeys.MaxStaff, () => Task.FromResult(25), "Staff");

        // Nothing refused this before: max_staff was displayed and billed against but never
        // enforced on create, so any tenant could exceed the seats they had paid for.
        result.Should().BeOfType<Microsoft.AspNetCore.Mvc.ObjectResult>()
            .Which.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task SeatGuard_AllowsUnlimited()
    {
        SeedPlanAndSubscription();

        var result = await Upkilo.API.Helpers.SeatLimitGuard.CheckAsync(
            _sut, _tenantId, FeatureKeys.MaxClients, () => Task.FromResult(1_000_000), "Client");

        result.Should().BeNull();
    }

    [Fact]
    public async Task SeatGuard_CountsPurchasedExpansionSeats()
    {
        SeedPlanAndSubscription(extraStaff: 5);

        // 25 plan seats + 5 bought = 30, so the 30th existing seat still blocks but 29 does not.
        (await Upkilo.API.Helpers.SeatLimitGuard.CheckAsync(
            _sut, _tenantId, FeatureKeys.MaxStaff, () => Task.FromResult(29), "Staff"))
            .Should().BeNull();

        (await Upkilo.API.Helpers.SeatLimitGuard.CheckAsync(
            _sut, _tenantId, FeatureKeys.MaxStaff, () => Task.FromResult(30), "Staff"))
            .Should().NotBeNull();
    }

    [Fact]
    public async Task SeatGuard_RefusesClientCreateAtTheTierBoundary()
    {
        SeedPlanAndSubscription();
        // Override the plan's unlimited max_clients down to a Free-like boundary.
        AddOverride(FeatureKeys.MaxClients, enabled: true, limit: 150);

        (await Upkilo.API.Helpers.SeatLimitGuard.CheckAsync(
            _sut, _tenantId, FeatureKeys.MaxClients, () => Task.FromResult(149), "Client"))
            .Should().BeNull();

        // max_clients was published on the pricing page and shown in billing, but no code path
        // — not even the downgrade handler — ever refused a record against it.
        (await Upkilo.API.Helpers.SeatLimitGuard.CheckAsync(
            _sut, _tenantId, FeatureKeys.MaxClients, () => Task.FromResult(150), "Client"))
            .Should().NotBeNull();
    }

    [Fact]
    public async Task SeatGuard_RefusesWhenSubscriptionIsInactive()
    {
        SeedPlanAndSubscription(SubscriptionStatus.Cancelled);

        var result = await Upkilo.API.Helpers.SeatLimitGuard.CheckAsync(
            _sut, _tenantId, FeatureKeys.MaxStaff, () => Task.FromResult(0), "Staff");

        // A cancelled subscription is a deliberate business state, not missing data.
        result.Should().NotBeNull("a cancelled tenant is entitled to zero seats, not unlimited");
    }

    [Fact]
    public async Task SeatGuard_AllowsWhenTenantHasNoSubscriptionRow()
    {
        // No SeedPlanAndSubscription() — this tenant has no billing data at all.
        var result = await Upkilo.API.Helpers.SeatLimitGuard.CheckAsync(
            _sut, _tenantId, FeatureKeys.MaxStaff, () => Task.FromResult(500), "Staff");

        // Registration writes PricingPlanId from `freePricingPlan?.Id`, which is nullable, so a
        // tenant provisioned before the catalogue was seeded has no resolvable plan. Refusing
        // would leave a working business unable to add a single staff member because of OUR
        // missing row — a self-inflicted outage, and a worse failure than the overage.
        result.Should().BeNull("missing entitlement data must not lock a tenant out of their own product");
    }

    [Fact]
    public async Task SeatGuard_AllowsWhenPlanNeverMappedTheLimit()
    {
        SeedPlanAndSubscription();

        // The seeded plan in this fixture maps max_staff and max_clients but not max_locations,
        // which resolves as PlanExcluded — a catalogue gap rather than a decision to sell zero.
        var result = await Upkilo.API.Helpers.SeatLimitGuard.CheckAsync(
            _sut, _tenantId, FeatureKeys.MaxLocations, () => Task.FromResult(99), "Location");

        result.Should().BeNull();
    }

    // ── Unknown keys ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData("AiFeatures")]        // the invented names every gate used to pass
    [InlineData("ApiAccess")]
    [InlineData("WhiteLabelDomain")]
    [InlineData("CustomBranding")]
    [InlineData("Webhooks")]
    [InlineData("AiCopilot")]
    public async Task LegacyPascalCaseNames_AreRejectedNotSilentlyFolded(string legacyName)
    {
        SeedPlanAndSubscription();

        // These must NOT resolve. Folding "AiCopilot" onto "ai_copilot" would paper over the
        // original defect and turn it into a leak the next time a name is mistyped.
        (await _sut.HasFeatureAsync(_tenantId, legacyName)).Should().BeFalse();
    }

    // ── Caching ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Invalidate_MakesTheNextReadSeeTheChange()
    {
        SeedPlanAndSubscription();
        (await _sut.HasFeatureAsync(_tenantId, FeatureKeys.AdvancedSecurity)).Should().BeFalse();

        AddOverride(FeatureKeys.AdvancedSecurity, enabled: true);
        await _sut.InvalidateAsync(_tenantId);

        (await _sut.HasFeatureAsync(_tenantId, FeatureKeys.AdvancedSecurity)).Should().BeTrue();
    }

    [Fact]
    public async Task InvalidateAll_StrandsEveryCachedEntry()
    {
        SeedPlanAndSubscription();
        (await _sut.HasFeatureAsync(_tenantId, FeatureKeys.AdvancedSecurity)).Should().BeFalse();

        AddOverride(FeatureKeys.AdvancedSecurity, enabled: true);
        // The admin-edits-a-plan path: no per-tenant key to remove, so the epoch moves instead.
        await _sut.InvalidateAllAsync();

        (await _sut.HasFeatureAsync(_tenantId, FeatureKeys.AdvancedSecurity)).Should().BeTrue();
    }

    [Fact]
    public async Task ScheduledOverride_TakesEffectWithoutWaitingForTheCacheToAgeOut()
    {
        SeedPlanAndSubscription();
        AddOverride(FeatureKeys.AdvancedSecurity, enabled: true,
            startsAt: DateTime.UtcNow.AddSeconds(1));

        // Warms the cache with a snapshot in which the override is not yet active — and, being
        // filtered out of the resolved features entirely, leaves no trace that it is pending.
        (await _sut.HasFeatureAsync(_tenantId, FeatureKeys.AdvancedSecurity)).Should().BeFalse();

        await Task.Delay(1400);

        // Without NextTransitionAt the cached snapshot would keep answering false until the
        // 5-minute entry aged out, so a grant scheduled for a specific time would silently
        // start late.
        (await _sut.HasFeatureAsync(_tenantId, FeatureKeys.AdvancedSecurity)).Should().BeTrue();
    }

    [Fact]
    public async Task NextTransition_IsTheNearestPendingChange()
    {
        SeedPlanAndSubscription();
        AddOverride(FeatureKeys.AdvancedSecurity, enabled: true,
            expiresAt: DateTime.UtcNow.AddDays(5));
        AddOverride(FeatureKeys.MarketingAutomation, enabled: true,
            startsAt: DateTime.UtcNow.AddDays(2));

        var set = await _sut.GetEffectiveEntitlementsAsync(_tenantId);

        set.NextTransitionAt.Should().NotBeNull();
        // The scheduled start at +2d is nearer than the expiry at +5d.
        set.NextTransitionAt!.Value.Should().BeCloseTo(DateTime.UtcNow.AddDays(2), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task NextTransition_IsNullWhenNothingIsPending()
    {
        SeedPlanAndSubscription();
        AddOverride(FeatureKeys.AdvancedSecurity, enabled: true); // permanent, no schedule

        var set = await _sut.GetEffectiveEntitlementsAsync(_tenantId);

        set.NextTransitionAt.Should().BeNull();
    }

    [Fact]
    public async Task CachedSnapshot_DoesNotOutliveAnOverrideExpiry()
    {
        SeedPlanAndSubscription();
        AddOverride(FeatureKeys.AdvancedSecurity, enabled: true, expiresAt: DateTime.UtcNow.AddSeconds(1));

        (await _sut.HasFeatureAsync(_tenantId, FeatureKeys.AdvancedSecurity)).Should().BeTrue();

        await Task.Delay(1200);

        // Without the read-time expiry re-check the 5-minute snapshot would keep this alive.
        (await _sut.HasFeatureAsync(_tenantId, FeatureKeys.AdvancedSecurity)).Should().BeFalse();
    }
}
