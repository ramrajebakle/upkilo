using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;
using Upkilo.Infrastructure.Services;
using Upkilo.Tests.Helpers;
using Xunit;

namespace Upkilo.Tests.Services;

/// <summary>
/// The rebooking job sends MARKETING messages, so the consent gate is the behaviour most worth
/// pinning down: sending without opt-in is unlawful under GDPR/PECR, CASL and the TCPA, and the
/// damage lands on the tenant's sending reputation. These tests exist so that gate cannot be
/// removed or inverted without a test going red.
/// </summary>
public class RebookReminderJobTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory;
    private readonly Mock<IEmailService> _email = new();
    private readonly Mock<ISmsService> _sms = new();

    public RebookReminderJobTests()
    {
        _dbFactory = new TestDbContextFactory();
        _sms.Setup(s => s.SendSmsAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>()))
            .ReturnsAsync(new SmsResult(true, "msg_test", null));
    }

    private RebookReminderJob CreateJob(AppDbContext context)
    {
        // The job resolves its dependencies from a scope, so the provider hands back the same
        // SQLite-backed context the test seeded.
        var services = new ServiceCollection();
        services.AddSingleton(context);
        services.AddSingleton(_email.Object);
        services.AddSingleton(_sms.Object);
        var provider = services.BuildServiceProvider();

        return new RebookReminderJob(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Mock.Of<ILogger<RebookReminderJob>>());
    }

    /// <summary>Seeds one completed booking that is overdue for a rebook.</summary>
    private static (Guid tenantId, Guid bookingId) Seed(
        AppDbContext ctx,
        bool marketingConsent,
        bool smsConsent,
        int rebookAfterDays = 30,
        int daysSinceVisit = 60,
        string? email = "client@example.com",
        string? phone = "+15550000000")
    {
        var tenantId = Guid.NewGuid();
        ctx.Tenants.Add(new Tenant { Id = tenantId, Name = "Test Salon", Slug = $"t{Guid.NewGuid():N}" });

        var client = new Client
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FirstName = "Ada",
            Email = email,
            Phone = phone,
            MarketingConsent = marketingConsent,
            SmsConsent = smsConsent,
        };
        ctx.Clients.Add(client);

        var service = new Service
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "Colour",
            DurationMinutes = 60,
            Price = 100m,
            RebookAfterDays = rebookAfterDays,
        };
        ctx.Services.Add(service);

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ClientId = client.Id,
            ServiceId = service.Id,
            Status = BookingStatus.Completed,
            StartTime = DateTime.UtcNow.AddDays(-daysSinceVisit),
            EndTime = DateTime.UtcNow.AddDays(-daysSinceVisit).AddHours(1),
        };
        ctx.Bookings.Add(booking);
        ctx.SaveChanges();

        return (tenantId, booking.Id);
    }

    [Fact]
    public async Task Sends_email_when_client_has_marketing_consent()
    {
        using var ctx = _dbFactory.CreateContext();
        Seed(ctx, marketingConsent: true, smsConsent: false);

        await CreateJob(ctx).RunAsync(CancellationToken.None);

        _email.Verify(e => e.SendSystemEmailAsync("client@example.com", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Sends_nothing_when_client_has_withheld_consent()
    {
        using var ctx = _dbFactory.CreateContext();
        Seed(ctx, marketingConsent: false, smsConsent: false);

        await CreateJob(ctx).RunAsync(CancellationToken.None);

        _email.Verify(e => e.SendSystemEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _sms.Verify(s => s.SendSmsAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>()), Times.Never);
    }

    [Fact]
    public async Task Falls_back_to_sms_only_when_no_email_address_exists()
    {
        using var ctx = _dbFactory.CreateContext();
        Seed(ctx, marketingConsent: true, smsConsent: true, email: null);

        await CreateJob(ctx).RunAsync(CancellationToken.None);

        _sms.Verify(s => s.SendSmsAsync(It.IsAny<Guid>(), "+15550000000", It.IsAny<string>(), It.IsAny<Guid?>()), Times.Once);
    }

    [Fact]
    public async Task Does_not_send_twice_for_the_same_visit()
    {
        using var ctx = _dbFactory.CreateContext();
        Seed(ctx, marketingConsent: true, smsConsent: false);
        var job = CreateJob(ctx);

        await job.RunAsync(CancellationToken.None);
        await job.RunAsync(CancellationToken.None);

        _email.Verify(e => e.SendSystemEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Does_not_send_before_the_interval_has_elapsed()
    {
        using var ctx = _dbFactory.CreateContext();
        // Visited 10 days ago, due after 30 — not yet due.
        Seed(ctx, marketingConsent: true, smsConsent: false, rebookAfterDays: 30, daysSinceVisit: 10);

        await CreateJob(ctx).RunAsync(CancellationToken.None);

        _email.Verify(e => e.SendSystemEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Does_not_nudge_a_client_who_has_already_rebooked()
    {
        using var ctx = _dbFactory.CreateContext();
        var (tenantId, bookingId) = Seed(ctx, marketingConsent: true, smsConsent: false);

        var past = ctx.Bookings.Single(b => b.Id == bookingId);
        ctx.Bookings.Add(new Booking
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ClientId = past.ClientId,
            ServiceId = past.ServiceId,
            Status = BookingStatus.Confirmed,
            StartTime = DateTime.UtcNow.AddDays(3),
            EndTime = DateTime.UtcNow.AddDays(3).AddHours(1),
        });
        ctx.SaveChanges();

        await CreateJob(ctx).RunAsync(CancellationToken.None);

        _email.Verify(e => e.SendSystemEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Ignores_services_with_no_rebooking_interval()
    {
        using var ctx = _dbFactory.CreateContext();
        var (_, bookingId) = Seed(ctx, marketingConsent: true, smsConsent: false);
        var booking = ctx.Bookings.Single(b => b.Id == bookingId);
        var service = ctx.Services.Single(s => s.Id == booking.ServiceId);
        service.RebookAfterDays = null;
        ctx.SaveChanges();

        await CreateJob(ctx).RunAsync(CancellationToken.None);

        _email.Verify(e => e.SendSystemEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Ignores_visits_older_than_the_ninety_day_cutoff()
    {
        using var ctx = _dbFactory.CreateContext();
        // Switching the feature on must not mail years of dormant history in one night.
        Seed(ctx, marketingConsent: true, smsConsent: false, rebookAfterDays: 30, daysSinceVisit: 400);

        await CreateJob(ctx).RunAsync(CancellationToken.None);

        _email.Verify(e => e.SendSystemEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Sends_nothing_when_the_tenant_has_paused_reminders()
    {
        using var ctx = _dbFactory.CreateContext();
        var (tenantId, _) = Seed(ctx, marketingConsent: true, smsConsent: true);
        var tenant = ctx.Tenants.Single(t => t.Id == tenantId);
        tenant.RebookRemindersEnabled = false;
        ctx.SaveChanges();

        await CreateJob(ctx).RunAsync(CancellationToken.None);

        _email.Verify(e => e.SendSystemEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _sms.Verify(s => s.SendSmsAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>()), Times.Never);
    }

    [Fact]
    public async Task Rebooking_one_service_does_not_suppress_the_reminder_for_another()
    {
        using var ctx = _dbFactory.CreateContext();
        var (tenantId, bookingId) = Seed(ctx, marketingConsent: true, smsConsent: false);
        var colourVisit = ctx.Bookings.Single(b => b.Id == bookingId);

        // Same client, a DIFFERENT service, also overdue — and they have rebooked that other one.
        var massage = new Service
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "Massage",
            DurationMinutes = 60,
            Price = 80m,
            RebookAfterDays = 30,
        };
        ctx.Services.Add(massage);
        ctx.Bookings.Add(new Booking
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ClientId = colourVisit.ClientId,
            ServiceId = massage.Id,
            Status = BookingStatus.Completed,
            StartTime = DateTime.UtcNow.AddDays(-60),
            EndTime = DateTime.UtcNow.AddDays(-60).AddHours(1),
        });
        ctx.Bookings.Add(new Booking
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ClientId = colourVisit.ClientId,
            ServiceId = massage.Id,
            Status = BookingStatus.Confirmed,
            StartTime = DateTime.UtcNow.AddDays(5),
            EndTime = DateTime.UtcNow.AddDays(5).AddHours(1),
        });
        ctx.SaveChanges();

        await CreateJob(ctx).RunAsync(CancellationToken.None);

        // Targeting is per service: the massage is suppressed because it was rebooked, the colour
        // still goes out. Exactly one message, and it names the colour.
        _email.Verify(e => e.SendSystemEmailAsync(
            It.IsAny<string>(),
            It.Is<string>(subject => subject.Contains("Colour")),
            It.IsAny<string>()), Times.Once);
        _email.Verify(e => e.SendSystemEmailAsync(
            It.IsAny<string>(),
            It.Is<string>(subject => subject.Contains("Massage")),
            It.IsAny<string>()), Times.Never);
    }

    public void Dispose() => _dbFactory.Dispose();
}
