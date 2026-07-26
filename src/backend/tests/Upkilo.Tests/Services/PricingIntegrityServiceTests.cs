using FluentAssertions;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data;
using Upkilo.Infrastructure.Data.Seeders;
using Upkilo.Infrastructure.Services;
using Upkilo.Tests.Helpers;
using Xunit;

namespace Upkilo.Tests.Services;

/// <summary>
/// Guards the published pricing catalogue.
///
/// Pricing is uniquely bad at failing loudly: a missing or malformed price list still returns 200,
/// and the marketing page renders "Contact us" on every card. These tests assert the invariants
/// directly so a bad edit fails the build instead of quietly turning off signups.
/// </summary>
public class PricingIntegrityServiceTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory;

    public PricingIntegrityServiceTests() => _dbFactory = new TestDbContextFactory();
    public void Dispose() => _dbFactory.Dispose();

    private static PricingIntegrityService Sut(AppDbContext ctx) => new(ctx);

    private static async Task<AppDbContext> SeededAsync(TestDbContextFactory factory)
    {
        var ctx = factory.CreateContext();
        await PricingSeeder.SeedAsync(ctx);
        await ctx.SaveChangesAsync();
        return ctx;
    }

    // ── The shipped catalogue must be valid ──────────────────────────────

    [Fact]
    public async Task SeededCatalogue_HasNoCriticalIssues()
    {
        var ctx = await SeededAsync(_dbFactory);

        var issues = await Sut(ctx).ValidateAsync();

        issues.Where(i => i.Severity == PricingIssueSeverity.Critical)
              .Should().BeEmpty("the pricing we ship must be buyable");
    }

    [Fact]
    public async Task SeededCatalogue_PricesEveryPlanInUsdOnly()
    {
        // Pins the decision that Upkilo bills in a single currency. Re-adding a currency row
        // without revisiting that decision fails here rather than silently reintroducing the
        // drift that multi-currency pricing caused before.
        var ctx = await SeededAsync(_dbFactory);

        var currencies = ctx.PlanPrices.Select(p => p.CurrencyCode).Distinct().ToList();

        currencies.Should().OnlyContain(c => c == "USD");
    }

    [Fact]
    public async Task SeededCatalogue_EveryPaidPlanHasBothCycles()
    {
        var ctx = await SeededAsync(_dbFactory);

        var paidPlans = ctx.PricingPlans
            .Where(p => p.IsActive && !p.IsCustom)
            .Select(p => new { p.Name, Cycles = p.Prices.Select(x => x.Cycle).ToList() })
            .Where(p => p.Cycles.Count > 0)
            .ToList();

        paidPlans.Should().NotBeEmpty();
        foreach (var plan in paidPlans)
        {
            plan.Cycles.Should().Contain(BillingCycle.Monthly, $"'{plan.Name}' needs a monthly price");
            plan.Cycles.Should().Contain(BillingCycle.Annual, $"'{plan.Name}' needs an annual price");
        }
    }

    [Fact]
    public async Task SeededCatalogue_AnnualIsCheaperThanTwelveMonths()
    {
        // The pricing page advertises annual billing as a saving. If a digit slips, that claim
        // becomes false and customers are overcharged with nothing else noticing.
        var ctx = await SeededAsync(_dbFactory);

        var plans = ctx.PricingPlans.Where(p => p.Prices.Any()).Select(p => new
        {
            p.Name,
            Monthly = p.Prices.Where(x => x.Cycle == BillingCycle.Monthly).Select(x => (decimal?)x.Amount).FirstOrDefault(),
            Annual = p.Prices.Where(x => x.Cycle == BillingCycle.Annual).Select(x => (decimal?)x.Amount).FirstOrDefault()
        }).ToList();

        foreach (var p in plans.Where(x => x.Monthly.HasValue && x.Annual.HasValue))
            p.Annual!.Value.Should().BeLessThan(p.Monthly!.Value * 12, $"'{p.Name}' annual must save money");
    }

    // ── The validator must actually catch breakage ───────────────────────

    [Fact]
    public async Task EmptyCatalogue_IsReportedCritical()
    {
        var ctx = _dbFactory.CreateContext();

        var issues = await Sut(ctx).ValidateAsync();

        issues.Should().Contain(i => i.Code == "no_plans" && i.Severity == PricingIssueSeverity.Critical);
    }

    [Fact]
    public async Task PlansWithNoPriceRows_AreReportedCritical()
    {
        // The exact shape of a failed seed: plans exist, prices do not. The API still returns
        // 200 and the pricing page renders "Contact us" everywhere.
        var ctx = _dbFactory.CreateContext();
        ctx.PricingPlans.Add(new PricingPlan { Name = "Starter", IsActive = true, IsCustom = false });
        await ctx.SaveChangesAsync();

        var issues = await Sut(ctx).ValidateAsync();

        issues.Should().Contain(i => i.Code == "no_priced_plans" && i.Severity == PricingIssueSeverity.Critical);
    }

    [Fact]
    public async Task MissingAnnualPrice_IsReportedCritical()
    {
        var ctx = _dbFactory.CreateContext();
        var plan = new PricingPlan { Name = "Starter", IsActive = true, IsCustom = false };
        plan.Prices.Add(new PlanPrice { Amount = 39, CurrencyCode = "USD", Cycle = BillingCycle.Monthly });
        ctx.PricingPlans.Add(plan);
        await ctx.SaveChangesAsync();

        var issues = await Sut(ctx).ValidateAsync();

        issues.Should().Contain(i => i.Code == "missing_annual");
    }

    [Fact]
    public async Task AnnualCostingMoreThanTwelveMonths_IsReportedCritical()
    {
        var ctx = _dbFactory.CreateContext();
        var plan = new PricingPlan { Name = "Starter", IsActive = true, IsCustom = false };
        plan.Prices.Add(new PlanPrice { Amount = 39, CurrencyCode = "USD", Cycle = BillingCycle.Monthly });
        plan.Prices.Add(new PlanPrice { Amount = 4680, CurrencyCode = "USD", Cycle = BillingCycle.Annual }); // 10x slip
        ctx.PricingPlans.Add(plan);
        await ctx.SaveChangesAsync();

        var issues = await Sut(ctx).ValidateAsync();

        issues.Should().Contain(i => i.Code == "annual_not_discounted" && i.Severity == PricingIssueSeverity.Critical);
    }

    [Fact]
    public async Task DuplicatePriceRows_AreReportedCritical()
    {
        // Two rows for the same cycle make the amount charged depend on row order — the root
        // cause of the pricing page previously showing inconsistent figures.
        var ctx = _dbFactory.CreateContext();
        var plan = new PricingPlan { Name = "Starter", IsActive = true, IsCustom = false };
        plan.Prices.Add(new PlanPrice { Amount = 39, CurrencyCode = "USD", Cycle = BillingCycle.Monthly });
        plan.Prices.Add(new PlanPrice { Amount = 49, CurrencyCode = "USD", Cycle = BillingCycle.Monthly });
        plan.Prices.Add(new PlanPrice { Amount = 370, CurrencyCode = "USD", Cycle = BillingCycle.Annual });
        ctx.PricingPlans.Add(plan);
        await ctx.SaveChangesAsync();

        var issues = await Sut(ctx).ValidateAsync();

        issues.Should().Contain(i => i.Code == "duplicate_price" && i.Severity == PricingIssueSeverity.Critical);
    }

    [Fact]
    public async Task NonBillingCurrencyRow_IsReportedCritical()
    {
        var ctx = _dbFactory.CreateContext();
        var plan = new PricingPlan { Name = "Starter", IsActive = true, IsCustom = false };
        plan.Prices.Add(new PlanPrice { Amount = 39, CurrencyCode = "USD", Cycle = BillingCycle.Monthly });
        plan.Prices.Add(new PlanPrice { Amount = 370, CurrencyCode = "USD", Cycle = BillingCycle.Annual });
        plan.Prices.Add(new PlanPrice { Amount = 2999, CurrencyCode = "INR", Cycle = BillingCycle.Monthly });
        ctx.PricingPlans.Add(plan);
        await ctx.SaveChangesAsync();

        var issues = await Sut(ctx).ValidateAsync();

        issues.Should().Contain(i => i.Code == "unexpected_currency");
    }

    [Fact]
    public async Task ZeroAmountOnPaidPlan_IsReportedCritical()
    {
        var ctx = _dbFactory.CreateContext();
        var plan = new PricingPlan { Name = "Starter", IsActive = true, IsCustom = false };
        plan.Prices.Add(new PlanPrice { Amount = 0, CurrencyCode = "USD", Cycle = BillingCycle.Monthly });
        plan.Prices.Add(new PlanPrice { Amount = 370, CurrencyCode = "USD", Cycle = BillingCycle.Annual });
        ctx.PricingPlans.Add(plan);
        await ctx.SaveChangesAsync();

        var issues = await Sut(ctx).ValidateAsync();

        issues.Should().Contain(i => i.Code == "non_positive_amount");
    }

    [Fact]
    public async Task PartialStripeSync_IsReportedAsWarning()
    {
        // Half-linked pricing takes checkout down for the unlinked plans only — easy to miss,
        // because the pricing page still renders correctly.
        var ctx = _dbFactory.CreateContext();
        var plan = new PricingPlan { Name = "Starter", IsActive = true, IsCustom = false };
        plan.Prices.Add(new PlanPrice { Amount = 39, CurrencyCode = "USD", Cycle = BillingCycle.Monthly, StripePriceId = "price_live_1" });
        plan.Prices.Add(new PlanPrice { Amount = 370, CurrencyCode = "USD", Cycle = BillingCycle.Annual });
        ctx.PricingPlans.Add(plan);
        await ctx.SaveChangesAsync();

        var issues = await Sut(ctx).ValidateAsync();

        issues.Should().Contain(i => i.Code == "partial_stripe_sync" && i.Severity == PricingIssueSeverity.Warning);
    }

    [Fact]
    public async Task FreeAndCustomPlansWithoutPrices_AreNotFlagged()
    {
        // Free plans and the "Contact us" enterprise tier legitimately carry no price rows.
        // Flagging them would make the check noisy enough to be ignored.
        var ctx = _dbFactory.CreateContext();
        ctx.PricingPlans.Add(new PricingPlan { Name = "Free", IsActive = true, IsCustom = false });
        ctx.PricingPlans.Add(new PricingPlan { Name = "Enterprise", IsActive = true, IsCustom = true });

        var paid = new PricingPlan { Name = "Starter", IsActive = true, IsCustom = false };
        paid.Prices.Add(new PlanPrice { Amount = 39, CurrencyCode = "USD", Cycle = BillingCycle.Monthly });
        paid.Prices.Add(new PlanPrice { Amount = 370, CurrencyCode = "USD", Cycle = BillingCycle.Annual });
        ctx.PricingPlans.Add(paid);
        await ctx.SaveChangesAsync();

        var issues = await Sut(ctx).ValidateAsync();

        issues.Where(i => i.Severity == PricingIssueSeverity.Critical).Should().BeEmpty();
    }
}
