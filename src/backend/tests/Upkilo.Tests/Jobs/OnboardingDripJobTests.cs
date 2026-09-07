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
/// The 7-day onboarding nudge had never reached a single tenant in production. Two independent
/// reasons, both covered here: the job skipped any tenant whose Tenant.Email was null (which
/// registration never populated), and it selected from TenantOnboardingProgress rows that were
/// only created when somebody opened the dashboard checklist — so a tenant who signed up and
/// never came back, the exact audience, had no row to match.
/// </summary>
public class OnboardingDripJobTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory;
    private readonly Mock<IEmailService> _emailService = new();

    public OnboardingDripJobTests()
    {
        _dbFactory = new TestDbContextFactory();
    }

    public void Dispose() => _dbFactory.Dispose();

    private OnboardingDripJob CreateJob()
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => _dbFactory.CreateContext());
        services.AddScoped(_ => _emailService.Object);
        services.AddScoped<IConfiguration>(_ => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["APP_URL"] = "https://test.upkilo.local" })
            .Build());

        var provider = services.BuildServiceProvider();
        return new OnboardingDripJob(
            provider.GetRequiredService<IServiceScopeFactory>(),
            new Mock<ILogger<OnboardingDripJob>>().Object);
    }

    /// <summary>Seeds a tenant that signed up <paramref name="daysAgo"/> days ago and did nothing since.</summary>
    private async Task<Guid> SeedStalledTenantAsync(int daysAgo, string? tenantEmail, string ownerEmail)
    {
        var context = _dbFactory.CreateContext();
        var signedUpAt = DateTime.UtcNow.AddDays(-daysAgo);
        var tenantId = Guid.NewGuid();

        context.Tenants.Add(new Tenant
        {
            Id = tenantId,
            Name = "Stalled Co",
            Slug = $"stalled-{Guid.NewGuid():N}",
            Email = tenantEmail,
            Status = TenantStatus.Active,
            CreatedAt = signedUpAt
        });

        context.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Email = ownerEmail,
            PasswordHash = "x",
            FirstName = "Dana",
            LastName = "Owner",
            Role = UserRole.Owner,
            Status = UserStatus.Active,
            CreatedAt = signedUpAt
        });

        context.Set<TenantOnboardingProgress>().Add(new TenantOnboardingProgress
        {
            TenantId = tenantId,
            UserId = Guid.NewGuid(),
            CreatedAt = signedUpAt
        });

        await context.SaveChangesAsync();

        // AppDbContext.UpdateTimestampsAndTenantId stamps CreatedAt = UtcNow on every Added
        // BaseEntity, so the back-dated value above is overwritten on insert. Age the row in a
        // second pass, as an update, which that hook leaves alone. In production these rows age
        // on their own; this only exists to fast-forward the clock.
        var aging = _dbFactory.CreateContext();
        var progress = await aging.Set<TenantOnboardingProgress>().FirstAsync(p => p.TenantId == tenantId);
        progress.CreatedAt = signedUpAt;
        await aging.SaveChangesAsync();

        return tenantId;
    }

    [Fact]
    public async Task RunAsync_StalledTenantWithNoTenantEmail_StillReceivesTheNudge()
    {
        // Tenant.Email null is the state every tenant created before the registration fix is in.
        var tenantId = await SeedStalledTenantAsync(daysAgo: 9, tenantEmail: null, ownerEmail: "owner@example.com");

        await CreateJob().RunAsync(CancellationToken.None);

        _emailService.Verify(e => e.SendSystemEmailAsync(
            "owner@example.com",
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Once);

        var progress = await _dbFactory.CreateContext().Set<TenantOnboardingProgress>()
            .FirstAsync(p => p.TenantId == tenantId);
        progress.DripEmailSentAt.Should().NotBeNull();
    }

    [Fact]
    public async Task RunAsync_PrefersTenantEmailWhenPresent()
    {
        await SeedStalledTenantAsync(daysAgo: 9, tenantEmail: "billing@example.com", ownerEmail: "owner@example.com");

        await CreateJob().RunAsync(CancellationToken.None);

        _emailService.Verify(e => e.SendSystemEmailAsync(
            "billing@example.com", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task RunAsync_GreetsThePersonNotTheCompany()
    {
        await SeedStalledTenantAsync(daysAgo: 9, tenantEmail: "billing@example.com", ownerEmail: "owner@example.com");

        await CreateJob().RunAsync(CancellationToken.None);

        // Was "Hey Stalled Co! 👋" — the tenant's company name.
        _emailService.Verify(e => e.SendSystemEmailAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.Is<string>(body => body.Contains("Hey Dana!") && !body.Contains("Hey Stalled Co"))), Times.Once);
    }

    [Fact]
    public async Task RunAsync_LinksToTheConfiguredHostNotHardcodedProduction()
    {
        await SeedStalledTenantAsync(daysAgo: 9, tenantEmail: "billing@example.com", ownerEmail: "owner@example.com");

        await CreateJob().RunAsync(CancellationToken.None);

        _emailService.Verify(e => e.SendSystemEmailAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.Is<string>(body => body.Contains("https://test.upkilo.local/onboarding")
                                  && !body.Contains("https://app.upkilo.com"))), Times.Once);
    }

    [Fact]
    public async Task RunAsync_TenantOutsideTheWindow_IsNotEmailed()
    {
        await SeedStalledTenantAsync(daysAgo: 2, tenantEmail: "billing@example.com", ownerEmail: "owner@example.com");

        await CreateJob().RunAsync(CancellationToken.None);

        _emailService.Verify(e => e.SendSystemEmailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RunAsync_DoesNotEmailTheSameTenantTwice()
    {
        await SeedStalledTenantAsync(daysAgo: 9, tenantEmail: "billing@example.com", ownerEmail: "owner@example.com");

        var job = CreateJob();
        await job.RunAsync(CancellationToken.None);
        await job.RunAsync(CancellationToken.None);

        _emailService.Verify(e => e.SendSystemEmailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }
}
