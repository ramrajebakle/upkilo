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

public class GiftCertificateServiceTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory;
    private readonly Mock<ILogger<GiftCertificateService>> _loggerMock = new();

    public GiftCertificateServiceTests() => _dbFactory = new TestDbContextFactory();
    public void Dispose() => _dbFactory.Dispose();

    private (GiftCertificateService sut, Upkilo.Infrastructure.Data.AppDbContext ctx, Guid tenantId) CreateSut()
    {
        var ctx = _dbFactory.CreateContext();
        var tenantId = Guid.NewGuid();
        ctx.Tenants.Add(new Tenant { Id = tenantId, Name = "T", Slug = "t" });
        ctx.SaveChanges();
        return (new GiftCertificateService(ctx, _loggerMock.Object), ctx, tenantId);
    }

    [Fact]
    public async Task IssueGiftCertificateAsync_CreatesActiveCertificateWithCorrectAmount()
    {
        var (sut, _, tenantId) = CreateSut();

        var cert = await sut.IssueGiftCertificateAsync(tenantId, 100m, "recipient@test.com", "Alice");

        cert.Should().NotBeNull();
        cert.Code.Should().StartWith("UPK-");
        cert.InitialAmount.Should().Be(100m);
        cert.RemainingAmount.Should().Be(100m);
        cert.Status.Should().Be(GiftCertificateStatus.Active);
    }

    [Fact]
    public async Task ValidateCodeAsync_WhenCodeExists_ReturnsGiftCertificate()
    {
        var (sut, _, tenantId) = CreateSut();
        var cert = await sut.IssueGiftCertificateAsync(tenantId, 50m);

        var found = await sut.ValidateCodeAsync(tenantId, cert.Code);

        found.Should().NotBeNull();
        found!.Id.Should().Be(cert.Id);
    }

    [Fact]
    public async Task ValidateCodeAsync_WhenCodeNotFound_ReturnsNull()
    {
        var (sut, _, tenantId) = CreateSut();

        var result = await sut.ValidateCodeAsync(tenantId, "FAKE-CODE");

        result.Should().BeNull();
    }

    [Fact]
    public async Task RedeemAmountAsync_WhenSufficientBalance_DeductsAndReturnsTrue()
    {
        var (sut, ctx, tenantId) = CreateSut();
        var cert = await sut.IssueGiftCertificateAsync(tenantId, 100m);

        var result = await sut.RedeemAmountAsync(tenantId, cert.Code, 40m);

        result.Should().BeTrue();
        ctx.ChangeTracker.Clear();
        var updated = ctx.GiftCertificates.First(c => c.Id == cert.Id);
        updated.RemainingAmount.Should().Be(60m);
        updated.Status.Should().Be(GiftCertificateStatus.PartiallyRedeemed);
    }

    [Fact]
    public async Task RedeemAmountAsync_WhenFullyRedeemed_SetsStatusFullyRedeemed()
    {
        var (sut, ctx, tenantId) = CreateSut();
        var cert = await sut.IssueGiftCertificateAsync(tenantId, 50m);

        await sut.RedeemAmountAsync(tenantId, cert.Code, 50m);

        ctx.ChangeTracker.Clear();
        ctx.GiftCertificates.First(c => c.Id == cert.Id).Status.Should().Be(GiftCertificateStatus.FullyRedeemed);
    }

    [Fact]
    public async Task RedeemAmountAsync_WhenInsufficientBalance_ReturnsFalse()
    {
        var (sut, _, tenantId) = CreateSut();
        var cert = await sut.IssueGiftCertificateAsync(tenantId, 10m);

        var result = await sut.RedeemAmountAsync(tenantId, cert.Code, 50m);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task RedeemAmountAsync_WhenCodeInvalid_ReturnsFalse()
    {
        var (sut, _, tenantId) = CreateSut();

        var result = await sut.RedeemAmountAsync(tenantId, "BAD-CODE", 10m);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateCodeAsync_WhenExpired_SetsStatusToExpired()
    {
        var (sut, ctx, tenantId) = CreateSut();
        // Manually insert an expired certificate
        var expiredCert = new GiftCertificate
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Code = "UPK-EXPR-IRED",
            InitialAmount = 50m,
            RemainingAmount = 50m,
            Status = GiftCertificateStatus.Active,
            ExpiryDate = DateTime.UtcNow.AddDays(-1) // Already expired
        };
        ctx.GiftCertificates.Add(expiredCert);
        await ctx.SaveChangesAsync();

        var result = await sut.ValidateCodeAsync(tenantId, "UPK-EXPR-IRED");

        result.Should().NotBeNull();
        result!.Status.Should().Be(GiftCertificateStatus.Expired);
    }
}
