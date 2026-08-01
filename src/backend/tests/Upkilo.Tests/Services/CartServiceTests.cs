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

public class CartServiceTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory;
    private readonly Mock<ILogger<CartService>> _loggerMock = new();

    public CartServiceTests() => _dbFactory = new TestDbContextFactory();
    public void Dispose() => _dbFactory.Dispose();

    private (CartService sut, Upkilo.Infrastructure.Data.AppDbContext ctx, Guid tenantId, Product product) CreateSut()
    {
        var ctx = _dbFactory.CreateContext();
        var tenantId = Guid.NewGuid();
        ctx.Tenants.Add(new Tenant { Id = tenantId, Name = "T", Slug = "t" });
        var product = new Product
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "Widget",
            Price = 9.99m,
            IsActive = true,
            TrackInventory = false
        };
        ctx.Products.Add(product);
        ctx.SaveChanges();
        return (new CartService(ctx, _loggerMock.Object), ctx, tenantId, product);
    }

    [Fact]
    public async Task AddToCartAsync_NewItem_CreatesCartItemWithCorrectQuantity()
    {
        var (sut, ctx, tenantId, product) = CreateSut();

        var item = await sut.AddToCartAsync(tenantId, null, "sess-abc", product.Id, 2);

        item.Quantity.Should().Be(2);
        item.ProductId.Should().Be(product.Id);
        ctx.ChangeTracker.Clear();
        ctx.CartItems.Should().HaveCount(1);
    }

    [Fact]
    public async Task AddToCartAsync_ExistingItem_IncrementsQuantity()
    {
        var (sut, ctx, tenantId, product) = CreateSut();

        await sut.AddToCartAsync(tenantId, null, "sess-abc", product.Id, 1);
        await sut.AddToCartAsync(tenantId, null, "sess-abc", product.Id, 3);

        ctx.ChangeTracker.Clear();
        ctx.CartItems.First().Quantity.Should().Be(4);
    }

    [Fact]
    public async Task AddToCartAsync_WhenProductNotFound_ThrowsException()
    {
        var (sut, _, tenantId, _) = CreateSut();

        var act = () => sut.AddToCartAsync(tenantId, null, "sess", Guid.NewGuid(), 1);

        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task RemoveFromCartAsync_RemovesItem()
    {
        var (sut, ctx, tenantId, product) = CreateSut();
        await sut.AddToCartAsync(tenantId, null, "sess", product.Id, 2);

        await sut.RemoveFromCartAsync(tenantId, null, "sess", product.Id);

        ctx.ChangeTracker.Clear();
        ctx.CartItems.Should().BeEmpty();
    }

    [Fact]
    public async Task ClearCartAsync_RemovesAllItemsForSession()
    {
        var (sut, ctx, tenantId, product) = CreateSut();
        await sut.AddToCartAsync(tenantId, null, "sess", product.Id, 1);

        await sut.ClearCartAsync(tenantId, null, "sess");

        ctx.ChangeTracker.Clear();
        ctx.CartItems.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCartAsync_ReturnsItemsForSessionOnly()
    {
        var (sut, _, tenantId, product) = CreateSut();
        await sut.AddToCartAsync(tenantId, null, "sess-a", product.Id, 1);
        await sut.AddToCartAsync(tenantId, null, "sess-b", product.Id, 1);

        var cart = await sut.GetCartAsync(tenantId, null, "sess-a");

        cart.Should().HaveCount(1);
    }

    [Fact]
    public async Task UpdateQuantityAsync_WhenQuantityIsZero_RemovesItem()
    {
        var (sut, ctx, tenantId, product) = CreateSut();
        await sut.AddToCartAsync(tenantId, null, "sess", product.Id, 3);

        await sut.UpdateQuantityAsync(tenantId, null, "sess", product.Id, 0);

        ctx.ChangeTracker.Clear();
        ctx.CartItems.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateQuantityAsync_WhenQuantityPositive_UpdatesItem()
    {
        var (sut, ctx, tenantId, product) = CreateSut();
        await sut.AddToCartAsync(tenantId, null, "sess", product.Id, 1);

        await sut.UpdateQuantityAsync(tenantId, null, "sess", product.Id, 5);

        ctx.ChangeTracker.Clear();
        ctx.CartItems.First().Quantity.Should().Be(5);
    }
}
