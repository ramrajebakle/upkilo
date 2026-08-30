using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using Upkilo.API.Attributes;
using Upkilo.API.Middleware;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data.Seeders;
using Upkilo.Tests.Helpers;
using Xunit;

namespace Upkilo.Tests.Services;

/// <summary>
/// Guards the entitlement vocabulary against drift.
///
/// These are the tests whose absence let the original defect ship. Every gate in the product
/// asked for a feature name that the seeded catalogue did not contain — "AiFeatures",
/// "ApiAccess", "WhiteLabelDomain", "AiCopilot" against a database holding only "ai_copilot",
/// "api_access", "white_label" — so all seventeen [RequiresFeature] attributes and all five
/// frontend gates denied unconditionally, for every tenant on every plan including Enterprise.
///
/// It was invisible because nothing ever compared the three lists: the constants in code, the
/// names in the attributes, and the rows in the database. These tests compare them.
/// </summary>
public class EntitlementCatalogTests
{
    /// <summary>
    /// Every name passed to [RequiresFeature] anywhere in the API must be a catalogue key.
    ///
    /// This is the assertion that fails the build the moment someone hand-writes a gate name
    /// again. Scanning the assembly rather than a hand-maintained list means a gate added on a
    /// new controller is covered without anyone remembering to update this test.
    /// </summary>
    [Fact]
    public void EveryRequiresFeatureAttribute_UsesAKnownCatalogueKey()
    {
        var offenders = new List<string>();

        foreach (var type in typeof(RequiresFeatureAttribute).Assembly.GetTypes())
        {
            foreach (var attr in type.GetCustomAttributes<RequiresFeatureAttribute>(inherit: false))
            {
                if (!FeatureKeys.IsKnown(attr.FeatureName))
                    offenders.Add($"{type.Name} (class): '{attr.FeatureName}'");
            }

            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                foreach (var attr in method.GetCustomAttributes<RequiresFeatureAttribute>(inherit: false))
                {
                    if (!FeatureKeys.IsKnown(attr.FeatureName))
                        offenders.Add($"{type.Name}.{method.Name}: '{attr.FeatureName}'");
                }
            }
        }

        offenders.Should().BeEmpty(
            "a [RequiresFeature] name outside FeatureKeys can never match a PricingFeature row, " +
            "so the gate denies every tenant on every plan — including customers who have paid " +
            "for the feature. Use a FeatureKeys constant.");
    }

    /// <summary>
    /// Same guarantee for [FeatureGuard], which is applied to 20+ controllers and is the widest
    /// entitlement surface in the API — wider than [RequiresFeature]. Its keys were correct
    /// snake_case, but nothing enforced that, and it carried its own PascalCase translation
    /// table full of names ("ai_features", "webhooks", "white_label_domain", "custom_branding")
    /// that never existed in the catalogue and would silently deny anything routed through them.
    /// </summary>
    [Fact]
    public void EveryFeatureGuardAttribute_UsesAKnownCatalogueKey()
    {
        var offenders = new List<string>();

        foreach (var type in typeof(FeatureGuardAttribute).Assembly.GetTypes())
        {
            foreach (var attr in type.GetCustomAttributes<FeatureGuardAttribute>(inherit: false))
            {
                if (!FeatureKeys.IsKnown(FeatureGuardKey(attr)))
                    offenders.Add($"{type.Name} (class): '{FeatureGuardKey(attr)}'");
            }

            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                foreach (var attr in method.GetCustomAttributes<FeatureGuardAttribute>(inherit: false))
                {
                    if (!FeatureKeys.IsKnown(FeatureGuardKey(attr)))
                        offenders.Add($"{type.Name}.{method.Name}: '{FeatureGuardKey(attr)}'");
                }
            }
        }

        offenders.Should().BeEmpty(
            "a [FeatureGuard] key outside FeatureKeys can never match a PricingFeature row, so " +
            "the gate denies every tenant on every plan.");
    }

    /// <summary>
    /// FeatureGuardAttribute keeps its key in a private field; reading it via reflection avoids
    /// widening the type's public surface purely for a test.
    /// </summary>
    private static string FeatureGuardKey(FeatureGuardAttribute attr) =>
        (string)(typeof(FeatureGuardAttribute)
            .GetField("_featureKey", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(attr) ?? string.Empty);

    /// <summary>
    /// The FeatureGuard surface must stay wide. If this collapses, gates were removed rather
    /// than migrated, and the assertion above would pass while the product went ungated.
    /// </summary>
    [Fact]
    public void FeatureGuard_StillCoversTheControllerSurface()
    {
        var guarded = typeof(FeatureGuardAttribute).Assembly.GetTypes()
            .Count(t => t.GetCustomAttributes<FeatureGuardAttribute>(false).Any());

        guarded.Should().BeGreaterThanOrEqualTo(15);
    }

    /// <summary>
    /// At least one gate must exist. Without this, the assertion above passes vacuously if the
    /// attributes are ever refactored away or the assembly scan silently stops matching.
    /// </summary>
    [Fact]
    public void TheApi_ActuallyGatesSomething()
    {
        var gateCount = typeof(RequiresFeatureAttribute).Assembly.GetTypes()
            .Sum(t =>
                t.GetCustomAttributes<RequiresFeatureAttribute>(false).Count() +
                t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                 .Sum(m => m.GetCustomAttributes<RequiresFeatureAttribute>(false).Count()));

        gateCount.Should().BeGreaterThan(0);
    }

    /// <summary>
    /// The seeded catalogue and the code constants must describe the same feature set.
    ///
    /// A key in code but not the database is a gate that always denies. A key in the database
    /// but not in code is a feature nothing can ever check, and one an admin can grant an
    /// override on that will never take effect.
    /// </summary>
    [Fact]
    public async Task SeededCatalogue_MatchesFeatureKeysExactly()
    {
        using var dbFactory = new TestDbContextFactory();
        var context = dbFactory.CreateContext();

        await PricingSeeder.SeedAsync(context);

        var seeded = context.PricingFeatures.Select(f => f.Key).ToHashSet(StringComparer.Ordinal);

        seeded.Should().BeEquivalentTo(FeatureKeys.All);
    }

    /// <summary>
    /// Every seeded plan must state a position on every feature. A plan that simply omits a
    /// mapping resolves to "excluded", which is usually right by accident but means the
    /// catalogue no longer documents what a tier actually sells.
    /// </summary>
    [Fact]
    public async Task EverySeededPlan_MapsEveryFeature()
    {
        using var dbFactory = new TestDbContextFactory();
        var context = dbFactory.CreateContext();

        await PricingSeeder.SeedAsync(context);

        var plans = context.PricingPlans.ToList();
        plans.Should().NotBeEmpty();

        var mappings = context.PlanFeatureMappings.ToList();
        var features = context.PricingFeatures.ToDictionary(f => f.Id, f => f.Key);

        var gaps = new List<string>();
        foreach (var plan in plans)
        {
            var mapped = mappings
                .Where(m => m.PricingPlanId == plan.Id)
                .Select(m => features[m.PricingFeatureId])
                .ToHashSet(StringComparer.Ordinal);

            foreach (var key in FeatureKeys.All.Where(k => !mapped.Contains(k)))
                gaps.Add($"{plan.Name} has no mapping for '{key}'");
        }

        gaps.Should().BeEmpty();
    }

    /// <summary>
    /// The seeded catalogue must not contain duplicate keys — resolution picks one row per key,
    /// so duplicates would make a plan's entitlements depend on row order.
    /// </summary>
    [Fact]
    public async Task SeededCatalogue_HasNoDuplicateKeys()
    {
        using var dbFactory = new TestDbContextFactory();
        var context = dbFactory.CreateContext();

        await PricingSeeder.SeedAsync(context);

        var duplicates = context.PricingFeatures
            .AsEnumerable()
            .GroupBy(f => f.Key, StringComparer.Ordinal)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);

        duplicates.Should().BeEmpty();
    }

    /// <summary>
    /// The tier map must resolve every plan name PricingSeeder actually creates.
    ///
    /// Tenant.SubscriptionTier drives AI model selection, job quotas and rate limits. When
    /// "growth" was missing from this mapping after the pricing consolidation, every paying
    /// Growth customer fell through the default and was silently served the cheaper model and
    /// lower quota. Deriving the cases from the seeder rather than restating them means a new
    /// plan cannot be added without this failing.
    /// </summary>
    [Fact]
    public async Task TierMap_ResolvesEverySeededPlanName()
    {
        using var dbFactory = new TestDbContextFactory();
        var context = dbFactory.CreateContext();

        await PricingSeeder.SeedAsync(context);

        var unresolved = context.PricingPlans
            .Select(p => p.Name)
            .AsEnumerable()
            // Free legitimately maps to Free; anything else landing on the Free default is a
            // plan name the map has never been taught.
            .Where(name => !string.Equals(name, "Free", StringComparison.OrdinalIgnoreCase)
                        && SubscriptionTierMap.FromPlanName(name) == SubscriptionTier.Free)
            .ToList();

        unresolved.Should().BeEmpty(
            "a seeded plan that falls through to the default tier means paying customers get the " +
            "free tier's AI model, job quota and rate limit");
    }

    /// <summary>
    /// Legacy plan names still stored on live subscriptions must keep resolving to Growth, which
    /// is where those tiers were folded during the consolidation.
    /// </summary>
    [Theory]
    [InlineData("Professional")]
    [InlineData("Business")]
    [InlineData("Agency")]
    public void TierMap_KeepsLegacyPlanAliases(string legacyName)
        => SubscriptionTierMap.FromPlanName(legacyName).Should().Be(SubscriptionTier.Growth);

    /// <summary>
    /// An unrecognised plan name must NOT grant a paid tier. The previous inline map defaulted
    /// to Starter, so a typo or a hand-created plan silently handed out paid capacity.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("SomePlanWeNeverShipped")]
    public void TierMap_DefaultsToFree_NotAPaidTier(string? unknown)
        => SubscriptionTierMap.FromPlanName(unknown).Should().Be(SubscriptionTier.Free);

    /// <summary>
    /// The rate limiter reads its tier from the plan NAME, and plan names are not tier names.
    /// Enum.TryParse has no member called "Professional", so a customer on that legacy paid plan
    /// parsed to nothing and was throttled at the free-tier limit. The map knows the aliases.
    /// </summary>
    [Fact]
    public void TierMap_ResolvesLegacyNamesThatEnumParseCannot()
    {
        // Guards the specific substitution made in SubscriptionEnforcerMiddleware.
        Enum.TryParse<SubscriptionTier>("Professional", out _)
            .Should().BeFalse("this is why parsing the plan name was wrong");

        SubscriptionTierMap.FromPlanName("Professional").Should().Be(SubscriptionTier.Growth);
    }

    /// <summary>
    /// Numeric keys must be declared as such, so the admin override UI asks for a limit and the
    /// resolver treats NumericLimit as meaningful.
    /// </summary>
    [Fact]
    public void NumericKeys_AreAllPartOfTheCatalogue()
        => FeatureKeys.Numeric.Should().BeSubsetOf(FeatureKeys.All);
}
