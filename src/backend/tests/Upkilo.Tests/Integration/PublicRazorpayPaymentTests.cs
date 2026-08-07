using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data;
using Xunit;

namespace Upkilo.Tests.Integration;

/// <summary>
/// Boots the real DI graph (via OpenApiContractTests.ApiFactory) and exercises
/// PublicBookingController's razorpay/order and razorpay/verify actions over real HTTP.
///
/// This is the test that would have caught RazorpayService never being registered in
/// Program.cs: before that fix, resolving PublicBookingController for ANY action failed
/// with an ASP.NET dependency-resolution error the moment the constructor gained a
/// RazorpayService parameter — a raw unit test constructing the controller directly, or
/// a DbContext-only test like BookingIntegrationTests, would not have exercised that path
/// at all.
///
/// Deliberately does not assert a 200 from razorpay/order: this CI environment has no
/// RAZORPAY_KEY_ID/RAZORPAY_KEY_SECRET configured (only deploy.yml's staging/production
/// App Service settings reference them), so RazorpayService.CreateOrderAsync will
/// legitimately fail against the real Razorpay API regardless of whether the DI/routing
/// bug is fixed. Every case below instead exercises a path that is deterministic
/// regardless of whether real credentials exist — proving the controller is reachable
/// and its own validation logic runs, without depending on external configuration this
/// machine cannot supply.
/// </summary>
[Trait("Category", "Integration")]
public class PublicRazorpayPaymentTests : IClassFixture<OpenApiContractTests.ApiFactory>
{
    private readonly OpenApiContractTests.ApiFactory _factory;
    private readonly HttpClient _client;

    public PublicRazorpayPaymentTests(OpenApiContractTests.ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<(Tenant Tenant, Service Service, Booking Booking)> SeedBookingAsync(bool requiresPayment)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var tenantId = Guid.NewGuid();
        var tenant = new Tenant
        {
            Id = tenantId,
            Name = "Razorpay Test Salon",
            Slug = $"rzp-test-{tenantId:N}"[..24],
            IsActive = true
        };
        context.Tenants.Add(tenant);

        var service = new Service
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "Haircut",
            DurationMinutes = 30,
            Price = 500m,
            Currency = "INR",
            IsActive = true,
            RequiresPayment = requiresPayment,
            DepositAmount = requiresPayment ? 100m : null
        };
        context.Services.Add(service);

        var client = new Client
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FirstName = "Test",
            LastName = "Client",
            Email = $"{Guid.NewGuid():N}@example.com"
        };
        context.Clients.Add(client);
        await context.SaveChangesAsync();

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ClientId = client.Id,
            ServiceId = service.Id,
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(1).AddMinutes(30),
            Status = BookingStatus.Pending,
            Price = service.Price,
            Source = BookingSource.Website
        };
        context.Bookings.Add(booking);
        await context.SaveChangesAsync();

        return (tenant, service, booking);
    }

    [Fact]
    public async Task CreateRazorpayOrder_ServiceDoesNotRequirePayment_ReturnsBadRequest()
    {
        var (tenant, _, booking) = await SeedBookingAsync(requiresPayment: false);

        var response = await _client.PostAsJsonAsync(
            $"/api/booking/{tenant.Slug}/razorpay/order",
            new { bookingId = booking.Id });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateRazorpayOrder_BookingNotFound_ReturnsNotFound()
    {
        var (tenant, _, _) = await SeedBookingAsync(requiresPayment: false);

        var response = await _client.PostAsJsonAsync(
            $"/api/booking/{tenant.Slug}/razorpay/order",
            new { bookingId = Guid.NewGuid() });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateRazorpayOrder_TenantNotFound_ReturnsNotFound()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/booking/no-such-tenant-slug/razorpay/order",
            new { bookingId = Guid.NewGuid() });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task VerifyRazorpayPayment_InvalidSignature_ReturnsBadRequest()
    {
        // VerifySignature is pure HMAC-SHA256 math inside RazorpayService — no outbound call,
        // and it fails closed (returns false) when the signing secret is unconfigured, exactly
        // as it would for a genuinely forged signature. Deterministic either way.
        var (tenant, _, booking) = await SeedBookingAsync(requiresPayment: true);

        var response = await _client.PostAsJsonAsync(
            $"/api/booking/{tenant.Slug}/razorpay/verify",
            new
            {
                bookingId = booking.Id,
                orderId = "order_fake",
                paymentId = "pay_fake",
                signature = "not-a-real-signature"
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task VerifyRazorpayPayment_AlreadySucceeded_ReturnsSuccessWithoutRecapturing()
    {
        var (tenant, _, booking) = await SeedBookingAsync(requiresPayment: true);

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var tracked = await context.Bookings.FirstAsync(b => b.Id == booking.Id);
            tracked.PaymentStatus = PaymentStatus.Succeeded;
            await context.SaveChangesAsync();
        }

        var response = await _client.PostAsJsonAsync(
            $"/api/booking/{tenant.Slug}/razorpay/verify",
            new
            {
                bookingId = booking.Id,
                orderId = "order_fake",
                paymentId = "pay_fake",
                signature = "irrelevant-idempotent-path-returns-before-checking-this"
            });

        // The idempotency guard returns success before VerifySignature is ever called, so an
        // already-succeeded booking short-circuits regardless of the (garbage) signature above.
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
