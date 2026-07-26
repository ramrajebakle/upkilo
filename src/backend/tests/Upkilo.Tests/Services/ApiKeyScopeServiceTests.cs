using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Services;
using Upkilo.Tests.Helpers;
using Xunit;

namespace Upkilo.Tests.Services;

public class ApiKeyScopeServiceTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory;
    public ApiKeyScopeServiceTests() => _dbFactory = new TestDbContextFactory();
    public void Dispose() => _dbFactory.Dispose();

    private string HashKey(string key)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(key));
        return Convert.ToBase64String(bytes);
    }

    private async Task<(ApiKeyScopeService sut, string plainKey)> SeedApiKey(
        List<string> scopes, bool isActive = true, DateTime? expiresAt = null)
    {
        var ctx = _dbFactory.CreateContext();
        var tenantId = Guid.NewGuid();
        ctx.Tenants.Add(new Tenant { Id = tenantId, Name = "T", Slug = "t" });
        var plainKey = "sk_test_" + Guid.NewGuid().ToString("N");
        ctx.Set<ApiKey>().Add(new ApiKey
        {
            Id = Guid.NewGuid(), TenantId = tenantId, Name = "Test",
            KeyHash = HashKey(plainKey), Prefix = "sk_test",
            IsActive = isActive, Scopes = scopes, ExpiresAt = expiresAt
        });
        await ctx.SaveChangesAsync();
        return (new ApiKeyScopeService(ctx), plainKey);
    }

    [Fact]
    public async Task ValidateScopeAsync_WhenWildcardScope_ReturnsTrue()
    {
        var (sut, key) = await SeedApiKey(new List<string> { "*" });

        var result = await sut.ValidateScopeAsync(key, "read:bookings");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateScopeAsync_WhenMatchingScope_ReturnsTrue()
    {
        var (sut, key) = await SeedApiKey(new List<string> { "read:bookings", "write:clients" });

        var result = await sut.ValidateScopeAsync(key, "read:bookings");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateScopeAsync_WhenScopeNotInKey_ReturnsFalse()
    {
        var (sut, key) = await SeedApiKey(new List<string> { "read:bookings" });

        var result = await sut.ValidateScopeAsync(key, "write:bookings");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateScopeAsync_WhenKeyExpired_ReturnsFalse()
    {
        var (sut, key) = await SeedApiKey(
            new List<string> { "*" }, expiresAt: DateTime.UtcNow.AddDays(-1));

        var result = await sut.ValidateScopeAsync(key, "read:bookings");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateScopeAsync_WhenKeyInactive_ReturnsFalse()
    {
        var (sut, key) = await SeedApiKey(new List<string> { "*" }, isActive: false);

        var result = await sut.ValidateScopeAsync(key, "read:bookings");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateScopeAsync_WhenKeyNotFound_ReturnsFalse()
    {
        var ctx = _dbFactory.CreateContext();
        var sut = new ApiKeyScopeService(ctx);

        var result = await sut.ValidateScopeAsync("sk_nonexistent_key", "read:bookings");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateScopeAsync_WhenNoScopes_ReturnsFalse()
    {
        var (sut, key) = await SeedApiKey(new List<string>());

        var result = await sut.ValidateScopeAsync(key, "read:bookings");

        result.Should().BeFalse();
    }
}
