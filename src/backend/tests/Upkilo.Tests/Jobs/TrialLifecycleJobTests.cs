using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;
using Upkilo.Infrastructure.Services;
using Upkilo.Tests.Helpers;

namespace Upkilo.Tests.Jobs;

/// <summary>
/// The trial lifecycle: signup grants the top plan, reminders escalate at 7/3/1 days, expiry lands
/// the tenant on Free. None of this ran before — Tenant.TrialEndsAt was never set by any signup
/// path, so every trial check in the codebase silently evaluated to false.
/// </summary>
public class TrialLifecycleJobTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory;
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<IEntitlementService> _entitlements = new();

    public TrialLifecycleJobTests()
    {
        _dbFactory = new TestDbContextFactory();
    }

    public void Dispose() => _dbFactory.Dispose();

    private IServiceScopeFactory BuildScopeFactory()
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => _dbFactory.CreateContext());
        services.AddScoped(_ => _emailService.Object);
        services.AddScoped(_ => _entitlements.Object);
        services.AddScoped<IConfiguration>(_ => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["APP_URL"] = "https://test.upkilo.local" })
            .Build());
        return services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
    }

    private TrialExpiryJob CreateExpiryJob() =>
        new(BuildScopeFactory(), new Mock<ILogger<TrialExpiryJob>>().Object);

    private TrialReminderJob CreateReminderJob() =>
        new(BuildScopeFactory(), new Mock<ILogger<TrialReminderJob>>().Object);

    /// <summary>Seeds a tenant mid-trial, with the trial ending in <paramref name="trialEndsInDays"/> days.</summary>
    private async Task<(Guid TenantId, Guid FreePlanId)> SeedTrialingTenantAsync(double trialEndsInDays)
    {
        var context = _dbFactory.CreateContext();
        var tenantId = Guid.NewGuid();

        var free = new PricingPlan { Id = Guid.NewGuid(), Name = "Free", Description = "Free", IsActive = true };
        var growth = new PricingPlan { Id = Guid.NewGuid(), Name = "Growth", Description = "Growth", IsActive = true };
        context.PricingPlans.AddRange(free, growth);

        context.Tenants.Add(new Tenant
        {
            Id = tenantId,
            Name = "Trialing Co",
            Slug = $"trialing-{Guid.NewGuid():N}",
            Email = "owner@example.com",
            Status = TenantStatus.Active,
            PricingPlanId = growth.Id,
            SubscriptionTier = SubscriptionTier.Growth,
            TrialEndsAt = DateTime.UtcNow.AddDays(trialEndsInDays)
        });

        context.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Email = "owner@example.com",
            PasswordHash = "x",
            FirstName = "Sam",
            LastName = "Owner",
            Role = UserRole.Owner,
            Status = UserStatus.Active
        });

        context.Subscriptions.Add(new Subscription
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PricingPlanId = growth.Id,
            Status = SubscriptionStatus.Trialing,
            StartDate = DateTime.UtcNow.AddDays(trialEndsInDays - 14),
            EndDate = DateTime.UtcNow.AddDays(trialEndsInDays),
            AiMonthlyBudget = 5.00m
        });

        await context.SaveChangesAsync();
        return (tenantId, free.Id);
    }

    // ---- Expiry ----

    [Fact]
    public async Task ExpiryJob_ExpiredTrial_DowngradesToFreeAndKeepsTheAccount()
    {
        var (tenantId, freePlanId) = await SeedTrialingTenantAsync(trialEndsInDays: -1);

        await CreateExpiryJob().RunAsync(CancellationToken.None);

        var context = _dbFactory.CreateContext();
        var tenant = await context.Tenants.FirstAsync(t => t.Id == tenantId);
        var subscription = await context.Subscriptions.FirstAsync(s => s.TenantId == tenantId);

        tenant.SubscriptionTier.Should().Be(SubscriptionTier.Free);
        tenant.PricingPlanId.Should().Be(freePlanId);
        subscription.PricingPlanId.Should().Be(freePlanId);
        // Free is a plan, not a suspension — the subscription is Active on it.
        subscription.Status.Should().Be(SubscriptionStatus.Active);
        // The tenant is not suspended: their booking page must keep taking bookings.
        tenant.Status.Should().Be(TenantStatus.Active);
        // Retained as the record that this tenant had a trial and when it lapsed.
        tenant.TrialEndsAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ExpiryJob_InvalidatesTheEntitlementCache()
    {
        var (tenantId, _) = await SeedTrialingTenantAsync(trialEndsInDays: -1);

        await CreateExpiryJob().RunAsync(CancellationToken.None);

        // Without this the tenant keeps paid features until the cached snapshot happens to expire,
        // which is exactly the window a downgrade must not have.
        _entitlements.Verify(e => e.InvalidateAsync(tenantId), Times.Once);
    }

    [Fact]
    public async Task ExpiryJob_EmailsTheOwnerThatDataIsIntact()
    {
        await SeedTrialingTenantAsync(trialEndsInDays: -1);

        await CreateExpiryJob().RunAsync(CancellationToken.None);

        _emailService.Verify(e => e.SendSystemEmailAsync(
            "owner@example.com",
            It.IsAny<string>(),
            It.Is<string>(body => body.Contains("Nothing has been deleted") && body.Contains("Free plan"))),
            Times.Once);
    }

    [Fact]
    public async Task ExpiryJob_TrialStillRunning_DoesNothing()
    {
        var (tenantId, _) = await SeedTrialingTenantAsync(trialEndsInDays: 5);

        await CreateExpiryJob().RunAsync(CancellationToken.None);

        var tenant = await _dbFactory.CreateContext().Tenants.FirstAsync(t => t.Id == tenantId);
        tenant.SubscriptionTier.Should().Be(SubscriptionTier.Growth);
        _emailService.Verify(e => e.SendSystemEmailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExpiryJob_IsIdempotent()
    {
        await SeedTrialingTenantAsync(trialEndsInDays: -1);

        var job = CreateExpiryJob();
        await job.RunAsync(CancellationToken.None);
        await job.RunAsync(CancellationToken.None);

        // Second pass finds Status == Active, not Trialing, so it does not re-downgrade or re-mail.
        _emailService.Verify(e => e.SendSystemEmailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    /// <summary>
    /// A tenant who upgraded mid-trial has Status = Active while TrialEndsAt may still be in the
    /// past. Expiring them would downgrade a paying customer.
    /// </summary>
    [Fact]
    public async Task ExpiryJob_TenantWhoUpgradedMidTrial_IsNotTouched()
    {
        var (tenantId, _) = await SeedTrialingTenantAsync(trialEndsInDays: -1);

        var upgrade = _dbFactory.CreateContext();
        var sub = await upgrade.Subscriptions.FirstAsync(s => s.TenantId == tenantId);
        sub.Status = SubscriptionStatus.Active;
        await upgrade.SaveChangesAsync();

        await CreateExpiryJob().RunAsync(CancellationToken.None);

        var tenant = await _dbFactory.CreateContext().Tenants.FirstAsync(t => t.Id == tenantId);
        tenant.SubscriptionTier.Should().Be(SubscriptionTier.Growth);
    }

    // ---- Reminders ----

    [Theory]
    [InlineData(6.5, "days left in your Upkilo trial")]   // 7-day milestone
    [InlineData(2.5, "Your Upkilo trial ends in")]        // 3-day milestone
    [InlineData(0.5, "ends tomorrow")]                    // 1-day milestone
    public async Task ReminderJob_SendsTheRightMilestone(double daysLeft, string expectedSubjectFragment)
    {
        await SeedTrialingTenantAsync(trialEndsInDays: daysLeft);

        await CreateReminderJob().RunAsync(CancellationToken.None);

        _emailService.Verify(e => e.SendSystemEmailAsync(
            "owner@example.com",
            It.Is<string>(subject => subject.Contains(expectedSubjectFragment)),
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ReminderJob_DoesNotResendTheSameMilestone()
    {
        await SeedTrialingTenantAsync(trialEndsInDays: 2.5);

        var job = CreateReminderJob();
        await job.RunAsync(CancellationToken.None);
        await job.RunAsync(CancellationToken.None);

        // The original implementation of this de-dup mutated Tenant.Metadata in place, which EF
        // does not detect as a change — so it never persisted and the email would have re-sent on
        // every pass. The dictionary is reassigned now.
        _emailService.Verify(e => e.SendSystemEmailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ReminderJob_EarlyInTrial_SendsNothing()
    {
        await SeedTrialingTenantAsync(trialEndsInDays: 12);

        await CreateReminderJob().RunAsync(CancellationToken.None);

        _emailService.Verify(e => e.SendSystemEmailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    /// <summary>
    /// The reminders must not promise deletion. The account moves to Free and the data stays —
    /// saying otherwise is a lie the product has to live down the first time somebody checks.
    /// </summary>
    [Fact]
    public async Task ReminderJob_TellsTheTruthAboutWhatHappensAtExpiry()
    {
        await SeedTrialingTenantAsync(trialEndsInDays: 2.5);

        await CreateReminderJob().RunAsync(CancellationToken.None);

        _emailService.Verify(e => e.SendSystemEmailAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.Is<string>(body => body.Contains("Free plan") && !body.Contains("deleted"))),
            Times.Once);
    }
}
