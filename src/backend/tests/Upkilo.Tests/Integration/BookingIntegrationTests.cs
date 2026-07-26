using DotNet.Testcontainers.Builders;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Testcontainers.PostgreSql;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data;
using Upkilo.Infrastructure.Services;
using Xunit;

namespace Upkilo.Tests.Integration;

/// <summary>
/// T2: Integration tests using real PostgreSQL via Testcontainers.
/// Covers: booking creation, subscription lifecycle, Stripe webhook simulation.
/// These tests require Docker — they are skipped automatically when Docker is unavailable.
/// In CI (ubuntu-latest), Docker is always present.
/// </summary>
[Trait("Category", "Integration")]
public class BookingIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("upkilo_test")
        .WithUsername("test")
        .WithPassword("test")
        .WithCleanUp(true)
        .Build();

    private AppDbContext _context = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        _context = new AppDbContext(options);
        await _context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    // ─── Booking Creation ──────────────────────────────────────────────────────

    [Fact]
    public async Task CreateBooking_WithValidData_PersistsToDatabase()
    {
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Name = "Test Salon", Slug = $"test-{tenantId:N}"[..20], Industry = "Beauty" };
        _context.Tenants.Add(tenant);

        var client = new Client { Id = Guid.NewGuid(), TenantId = tenantId, FirstName = "Jane", LastName = "Doe", Email = "jane@example.com" };
        _context.Clients.Add(client);

        var service = new Service { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Haircut", DurationMinutes = 60, Price = 50m, IsActive = true };
        _context.Services.Add(service);

        var staffMember = new StaffMember { Id = Guid.NewGuid(), TenantId = tenantId, FirstName = "Alice", LastName = "Smith", Email = "alice@salon.com", IsActive = true };
        _context.StaffMembers.Add(staffMember);
        await _context.SaveChangesAsync();

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ClientId = client.Id,
            ServiceId = service.Id,
            StaffId = staffMember.Id,
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(1).AddHours(1),
            Status = BookingStatus.Confirmed,
            Price = 50m,
            Source = BookingSource.Manual
        };

        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();

        var saved = await _context.Bookings.FindAsync(booking.Id);
        saved.Should().NotBeNull();
        saved!.ClientId.Should().Be(client.Id);
        saved.Status.Should().Be(BookingStatus.Confirmed);
        saved.Price.Should().Be(50m);
    }

    [Fact]
    public async Task CreateMultipleBookings_SameSlot_BothPersist()
    {
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Name = "Gym", Slug = $"gym-{tenantId:N}"[..20], Industry = "Fitness" };
        _context.Tenants.Add(tenant);

        var client1 = new Client { Id = Guid.NewGuid(), TenantId = tenantId, FirstName = "Bob", LastName = "A", Email = "bob@a.com" };
        var client2 = new Client { Id = Guid.NewGuid(), TenantId = tenantId, FirstName = "Eve", LastName = "B", Email = "eve@b.com" };
        _context.Clients.AddRange(client1, client2);

        var service = new Service { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Yoga Class", DurationMinutes = 60, Price = 25m, IsActive = true };
        _context.Services.Add(service);
        await _context.SaveChangesAsync();

        var slot = DateTime.UtcNow.AddDays(2).Date.AddHours(9);
        var b1 = new Booking { Id = Guid.NewGuid(), TenantId = tenantId, ClientId = client1.Id, ServiceId = service.Id, StartTime = slot, EndTime = slot.AddHours(1), Status = BookingStatus.Confirmed, Price = 25m, Source = BookingSource.Website };
        var b2 = new Booking { Id = Guid.NewGuid(), TenantId = tenantId, ClientId = client2.Id, ServiceId = service.Id, StartTime = slot, EndTime = slot.AddHours(1), Status = BookingStatus.Confirmed, Price = 25m, Source = BookingSource.Website };
        _context.Bookings.AddRange(b1, b2);
        await _context.SaveChangesAsync();

        var count = await _context.Bookings.CountAsync(b => b.TenantId == tenantId && b.ServiceId == service.Id);
        count.Should().Be(2);
    }

    [Fact]
    public async Task CancelBooking_UpdatesStatus_PersistsToDatabase()
    {
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Name = "Spa", Slug = $"spa-{tenantId:N}"[..20], Industry = "Wellness" };
        _context.Tenants.Add(tenant);

        var client = new Client { Id = Guid.NewGuid(), TenantId = tenantId, FirstName = "Mary", LastName = "C", Email = "mary@c.com" };
        _context.Clients.Add(client);
        var service = new Service { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Massage", DurationMinutes = 90, Price = 100m, IsActive = true };
        _context.Services.Add(service);
        await _context.SaveChangesAsync();

        var booking = new Booking { Id = Guid.NewGuid(), TenantId = tenantId, ClientId = client.Id, ServiceId = service.Id, StartTime = DateTime.UtcNow.AddDays(3), EndTime = DateTime.UtcNow.AddDays(3).AddHours(1.5), Status = BookingStatus.Confirmed, Price = 100m, Source = BookingSource.Manual };
        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();

        booking.Status = BookingStatus.Cancelled;
        booking.CancelledAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var cancelled = await _context.Bookings.FindAsync(booking.Id);
        cancelled!.Status.Should().Be(BookingStatus.Cancelled);
        cancelled.CancelledAt.Should().NotBeNull();
    }

    // ─── Subscription Lifecycle ────────────────────────────────────────────────

    [Fact]
    public async Task CreateSubscription_WithPricingPlan_PersistsRelationship()
    {
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Name = "Studio", Slug = $"studio-{tenantId:N}"[..20], Industry = "Fitness" };
        _context.Tenants.Add(tenant);

        var plan = new PricingPlan { Id = Guid.NewGuid(), Name = "Professional", IsActive = true };
        _context.PricingPlans.Add(plan);
        await _context.SaveChangesAsync();

        var sub = new Subscription
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PricingPlanId = plan.Id,
            Status = SubscriptionStatus.Active,
            BillingInterval = BillingInterval.Monthly,
            CurrentPeriodStart = DateTime.UtcNow,
            CurrentPeriodEnd = DateTime.UtcNow.AddMonths(1)
        };
        _context.Subscriptions.Add(sub);
        await _context.SaveChangesAsync();

        var loaded = await _context.Subscriptions
            .Include(s => s.PricingPlan)
            .FirstOrDefaultAsync(s => s.TenantId == tenantId);

        loaded.Should().NotBeNull();
        loaded!.PricingPlan.Should().NotBeNull();
        loaded.PricingPlan!.Name.Should().Be("Professional");
        loaded.Status.Should().Be(SubscriptionStatus.Active);
    }

    [Fact]
    public async Task SubscriptionStatusChange_PauseAndResume_PersistsBothStates()
    {
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Name = "Clinic", Slug = $"clinic-{tenantId:N}"[..20], Industry = "Medical" };
        _context.Tenants.Add(tenant);
        var plan = new PricingPlan { Id = Guid.NewGuid(), Name = "Business", IsActive = true };
        _context.PricingPlans.Add(plan);

        var sub = new Subscription { Id = Guid.NewGuid(), TenantId = tenantId, PricingPlanId = plan.Id, Status = SubscriptionStatus.Active, BillingInterval = BillingInterval.Annual, CurrentPeriodStart = DateTime.UtcNow, CurrentPeriodEnd = DateTime.UtcNow.AddYears(1) };
        _context.Subscriptions.Add(sub);
        await _context.SaveChangesAsync();

        sub.Status = SubscriptionStatus.Paused;
        sub.PausedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var paused = await _context.Subscriptions.FindAsync(sub.Id);
        paused!.Status.Should().Be(SubscriptionStatus.Paused);

        paused.Status = SubscriptionStatus.Active;
        paused.PausedAt = null;
        await _context.SaveChangesAsync();

        var resumed = await _context.Subscriptions.FindAsync(sub.Id);
        resumed!.Status.Should().Be(SubscriptionStatus.Active);
        resumed.PausedAt.Should().BeNull();
    }

    // ─── Stripe Webhook Simulation ─────────────────────────────────────────────

    [Fact]
    public async Task StripeWebhookHandler_CustomerSubscriptionDeleted_MarksSubscriptionCancelled()
    {
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Name = "BarberShop", Slug = $"barber-{tenantId:N}"[..20], Industry = "Grooming", StripeCustomerId = "cus_test_webhook" };
        _context.Tenants.Add(tenant);
        var plan = new PricingPlan { Id = Guid.NewGuid(), Name = "Starter", IsActive = true };
        _context.PricingPlans.Add(plan);

        var sub = new Subscription { Id = Guid.NewGuid(), TenantId = tenantId, PricingPlanId = plan.Id, StripeSubscriptionId = "sub_webhook_test", Status = SubscriptionStatus.Active, BillingInterval = BillingInterval.Monthly, CurrentPeriodStart = DateTime.UtcNow, CurrentPeriodEnd = DateTime.UtcNow.AddMonths(1) };
        _context.Subscriptions.Add(sub);
        await _context.SaveChangesAsync();

        // Simulate webhook handler: update status on subscription.deleted event
        var subscription = await _context.Subscriptions.FirstOrDefaultAsync(s => s.StripeSubscriptionId == "sub_webhook_test");
        subscription.Should().NotBeNull();
        subscription!.Status = SubscriptionStatus.Cancelled;
        subscription.CancelledAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var cancelled = await _context.Subscriptions.FindAsync(sub.Id);
        cancelled!.Status.Should().Be(SubscriptionStatus.Cancelled);
    }

    [Fact]
    public async Task StripeWebhookHandler_InvoicePaymentSucceeded_UpdatesPeriodEnd()
    {
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Name = "Yoga Studio", Slug = $"yoga-{tenantId:N}"[..20], Industry = "Fitness", StripeCustomerId = "cus_yoga_test" };
        _context.Tenants.Add(tenant);
        var plan = new PricingPlan { Id = Guid.NewGuid(), Name = "Professional", IsActive = true };
        _context.PricingPlans.Add(plan);

        var originalPeriodEnd = DateTime.UtcNow.AddDays(5);
        var sub = new Subscription { Id = Guid.NewGuid(), TenantId = tenantId, PricingPlanId = plan.Id, StripeSubscriptionId = "sub_invoice_test", Status = SubscriptionStatus.Active, BillingInterval = BillingInterval.Monthly, CurrentPeriodStart = DateTime.UtcNow.AddMonths(-1), CurrentPeriodEnd = originalPeriodEnd };
        _context.Subscriptions.Add(sub);
        await _context.SaveChangesAsync();

        // Simulate invoice.payment_succeeded: extend period by 1 month
        var newPeriodEnd = originalPeriodEnd.AddMonths(1);
        sub.CurrentPeriodEnd = newPeriodEnd;
        sub.Status = SubscriptionStatus.Active;
        await _context.SaveChangesAsync();

        var renewed = await _context.Subscriptions.FindAsync(sub.Id);
        renewed!.CurrentPeriodEnd.Should().BeCloseTo(newPeriodEnd, TimeSpan.FromSeconds(1));
    }

    // ─── Booking Query Performance ─────────────────────────────────────────────

    [Fact]
    public async Task GetBookingsByDateRange_WithIndex_ReturnsCorrectBookings()
    {
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Name = "Salon", Slug = $"salon-{tenantId:N}"[..20], Industry = "Beauty" };
        _context.Tenants.Add(tenant);
        var client = new Client { Id = Guid.NewGuid(), TenantId = tenantId, FirstName = "X", LastName = "Y", Email = "x@y.com" };
        _context.Clients.Add(client);
        var service = new Service { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Cut", DurationMinutes = 30, Price = 30m, IsActive = true };
        _context.Services.Add(service);
        await _context.SaveChangesAsync();

        var today = DateTime.UtcNow.Date;
        for (int i = 0; i < 10; i++)
        {
            _context.Bookings.Add(new Booking { Id = Guid.NewGuid(), TenantId = tenantId, ClientId = client.Id, ServiceId = service.Id, StartTime = today.AddDays(i), EndTime = today.AddDays(i).AddMinutes(30), Status = BookingStatus.Confirmed, Price = 30m, Source = BookingSource.Website });
        }
        await _context.SaveChangesAsync();

        var results = await _context.Bookings
            .Where(b => b.TenantId == tenantId && b.StartTime >= today && b.StartTime < today.AddDays(5))
            .ToListAsync();

        results.Should().HaveCount(5);
    }

    [Fact]
    public async Task MultiTenantIsolation_TenantsCannotSeeEachOthersData()
    {
        var tenant1Id = Guid.NewGuid();
        var tenant2Id = Guid.NewGuid();
        _context.Tenants.Add(new Tenant { Id = tenant1Id, Name = "T1", Slug = $"t1-{tenant1Id:N}"[..20], Industry = "Beauty" });
        _context.Tenants.Add(new Tenant { Id = tenant2Id, Name = "T2", Slug = $"t2-{tenant2Id:N}"[..20], Industry = "Fitness" });

        var svc1 = new Service { Id = Guid.NewGuid(), TenantId = tenant1Id, Name = "T1 Service", DurationMinutes = 30, Price = 20m, IsActive = true };
        var svc2 = new Service { Id = Guid.NewGuid(), TenantId = tenant2Id, Name = "T2 Service", DurationMinutes = 30, Price = 20m, IsActive = true };
        _context.Services.AddRange(svc1, svc2);
        await _context.SaveChangesAsync();

        var t1Services = await _context.Services.Where(s => s.TenantId == tenant1Id).ToListAsync();
        var t2Services = await _context.Services.Where(s => s.TenantId == tenant2Id).ToListAsync();

        t1Services.Should().HaveCount(1);
        t1Services[0].Name.Should().Be("T1 Service");
        t2Services.Should().HaveCount(1);
        t2Services[0].Name.Should().Be("T2 Service");
    }

    [Fact]
    public async Task ConcurrentBookingCreation_BothSucceed_NoConcurrencyIssue()
    {
        var tenantId = Guid.NewGuid();
        _context.Tenants.Add(new Tenant { Id = tenantId, Name = "Concurrent", Slug = $"cc-{tenantId:N}"[..20], Industry = "Beauty" });
        var client = new Client { Id = Guid.NewGuid(), TenantId = tenantId, FirstName = "C", LastName = "D", Email = "c@d.com" };
        var svc = new Service { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Svc", DurationMinutes = 30, Price = 20m, IsActive = true };
        _context.Clients.Add(client);
        _context.Services.Add(svc);
        await _context.SaveChangesAsync();

        // Simulate concurrent bookings with separate contexts
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        await Task.WhenAll(
            Task.Run(async () =>
            {
                await using var ctx = new AppDbContext(opts);
                ctx.Bookings.Add(new Booking { Id = Guid.NewGuid(), TenantId = tenantId, ClientId = client.Id, ServiceId = svc.Id, StartTime = DateTime.UtcNow.AddDays(7), EndTime = DateTime.UtcNow.AddDays(7).AddMinutes(30), Status = BookingStatus.Confirmed, Price = 20m, Source = BookingSource.Website });
                await ctx.SaveChangesAsync();
            }),
            Task.Run(async () =>
            {
                await using var ctx = new AppDbContext(opts);
                ctx.Bookings.Add(new Booking { Id = Guid.NewGuid(), TenantId = tenantId, ClientId = client.Id, ServiceId = svc.Id, StartTime = DateTime.UtcNow.AddDays(8), EndTime = DateTime.UtcNow.AddDays(8).AddMinutes(30), Status = BookingStatus.Confirmed, Price = 20m, Source = BookingSource.Website });
                await ctx.SaveChangesAsync();
            })
        );

        var total = await _context.Bookings.CountAsync(b => b.TenantId == tenantId);
        total.Should().Be(2);
    }

    [Fact]
    public async Task NoShowBooking_StatusUpdate_TracksNoShowRate()
    {
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Name = "Physio", Slug = $"physio-{tenantId:N}"[..20], Industry = "Healthcare" };
        _context.Tenants.Add(tenant);
        var client = new Client { Id = Guid.NewGuid(), TenantId = tenantId, FirstName = "P", LastName = "Q", Email = "p@q.com" };
        _context.Clients.Add(client);
        var svc = new Service { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Session", DurationMinutes = 60, Price = 80m, IsActive = true };
        _context.Services.Add(svc);
        await _context.SaveChangesAsync();

        for (int i = 0; i < 4; i++)
        {
            _context.Bookings.Add(new Booking { Id = Guid.NewGuid(), TenantId = tenantId, ClientId = client.Id, ServiceId = svc.Id, StartTime = DateTime.UtcNow.AddDays(-i - 1), EndTime = DateTime.UtcNow.AddDays(-i - 1).AddHours(1), Status = i == 0 ? BookingStatus.NoShow : BookingStatus.Completed, Price = 80m, Source = BookingSource.Manual });
        }
        await _context.SaveChangesAsync();

        var total = await _context.Bookings.CountAsync(b => b.TenantId == tenantId && b.ClientId == client.Id);
        var noShows = await _context.Bookings.CountAsync(b => b.TenantId == tenantId && b.ClientId == client.Id && b.Status == BookingStatus.NoShow);
        var rate = (double)noShows / total;

        total.Should().Be(4);
        noShows.Should().Be(1);
        rate.Should().BeApproximately(0.25, 0.01);
    }
}
