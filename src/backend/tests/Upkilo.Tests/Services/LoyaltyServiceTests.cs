using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Services;
using Upkilo.Tests.Helpers;
using Xunit;

namespace Upkilo.Tests.Services;

public class LoyaltyServiceTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory;
    private readonly Mock<ILogger<LoyaltyService>> _loggerMock = new();

    public LoyaltyServiceTests() => _dbFactory = new TestDbContextFactory();
    public void Dispose() => _dbFactory.Dispose();

    private (LoyaltyService sut, Upkilo.Infrastructure.Data.AppDbContext ctx) CreateSut()
    {
        var ctx = _dbFactory.CreateContext();
        return (new LoyaltyService(ctx, _loggerMock.Object), ctx);
    }

    private async Task<Client> SeedClient(Upkilo.Infrastructure.Data.AppDbContext ctx, int initialPoints = 0)
    {
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "T", Slug = "t" };
        ctx.Tenants.Add(tenant);
        var client = new Client
        {
            Id = Guid.NewGuid(), TenantId = tenant.Id,
            FirstName = "Jane", LastName = "Doe",
            LoyaltyPoints = initialPoints, LoyaltyTier = "Bronze"
        };
        ctx.Clients.Add(client);
        await ctx.SaveChangesAsync();
        return client;
    }

    [Theory]
    [InlineData(100, 100)]
    [InlineData(49.99, 49)]
    [InlineData(0, 0)]
    public async Task CalculatePointsAsync_ReturnsFlooredPointsPerDollar(decimal amount, int expectedPoints)
    {
        var (sut, _) = CreateSut();
        var points = await sut.CalculatePointsAsync(amount);
        points.Should().Be(expectedPoints);
    }

    [Fact]
    public async Task AwardPointsAsync_IncreasesClientLoyaltyPoints()
    {
        var (sut, ctx) = CreateSut();
        var client = await SeedClient(ctx, initialPoints: 100);

        await sut.AwardPointsAsync(client.Id, 50, "Test award");

        ctx.ChangeTracker.Clear();
        var updated = ctx.Clients.First(c => c.Id == client.Id);
        updated.LoyaltyPoints.Should().Be(150);
    }

    [Fact]
    public async Task RedeemPointsAsync_DeductsPointsFromClient()
    {
        var (sut, ctx) = CreateSut();
        var client = await SeedClient(ctx, initialPoints: 500);

        await sut.RedeemPointsAsync(client.Id, 100, "Discount");

        ctx.ChangeTracker.Clear();
        ctx.Clients.First(c => c.Id == client.Id).LoyaltyPoints.Should().Be(400);
    }

    [Fact]
    public async Task RedeemPointsAsync_WhenInsufficientPoints_ThrowsInvalidOperationException()
    {
        var (sut, ctx) = CreateSut();
        var client = await SeedClient(ctx, initialPoints: 50);

        var act = () => sut.RedeemPointsAsync(client.Id, 200, "Too many");

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Insufficient*");
    }

    [Fact]
    public async Task RedeemPointsAsync_WhenBelowMinimum100_ThrowsInvalidOperationException()
    {
        var (sut, ctx) = CreateSut();
        var client = await SeedClient(ctx, initialPoints: 500);

        var act = () => sut.RedeemPointsAsync(client.Id, 50, "Too few");

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Minimum redemption*");
    }

    [Fact]
    public async Task UpdateClientTierAsync_WhenOver5000Points_SetsPlatinum()
    {
        var (sut, ctx) = CreateSut();
        var client = await SeedClient(ctx, initialPoints: 5000);

        await sut.UpdateClientTierAsync(client.Id);

        ctx.ChangeTracker.Clear();
        ctx.Clients.First(c => c.Id == client.Id).LoyaltyTier.Should().Be("Platinum");
    }

    [Fact]
    public async Task GetSummaryAsync_WhenClientNotFound_ReturnsBronzeDefaults()
    {
        var (sut, _) = CreateSut();

        var summary = await sut.GetSummaryAsync(Guid.NewGuid());

        summary.Tier.Should().Be("Bronze");
        summary.Points.Should().Be(0);
    }

    [Fact]
    public async Task AwardBookingPointsAsync_FirstBooking_AddsBonus()
    {
        var (sut, ctx) = CreateSut();
        var client = await SeedClient(ctx, initialPoints: 0);

        await sut.AwardBookingPointsAsync(client.Id, amountPaid: 100m, Guid.NewGuid(), isFirstBooking: true);

        ctx.ChangeTracker.Clear();
        // 100 pts from spend + 50 first-booking bonus = 150
        ctx.Clients.First(c => c.Id == client.Id).LoyaltyPoints.Should().Be(150);
    }
}
