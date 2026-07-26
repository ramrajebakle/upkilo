using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Services;
using Upkilo.Tests.Helpers;
using Xunit;

namespace Upkilo.Tests.Services;

public class ConsentServiceTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory;
    private readonly Mock<ILogger<ConsentService>> _loggerMock = new();

    public ConsentServiceTests() => _dbFactory = new TestDbContextFactory();
    public void Dispose() => _dbFactory.Dispose();

    private async Task SeedTenantAndClientAsync(Upkilo.Infrastructure.Data.AppDbContext ctx, Guid tenantId, Guid clientId)
    {
        ctx.Tenants.Add(new Tenant { Id = tenantId, Name = "T", Slug = "t-" + tenantId.ToString()[..8] });
        ctx.Clients.Add(new Client { Id = clientId, TenantId = tenantId, FirstName = "Jane", LastName = "Doe" });
        await ctx.SaveChangesAsync();
    }

    private async Task SeedTenantAndUserAsync(Upkilo.Infrastructure.Data.AppDbContext ctx, Guid tenantId, Guid userId)
    {
        ctx.Tenants.Add(new Tenant { Id = tenantId, Name = "T", Slug = "t-" + tenantId.ToString()[..8] });
        ctx.Users.Add(new User { Id = userId, TenantId = tenantId, FirstName = "John", LastName = "Doe", Email = $"user-{userId}@example.com", PasswordHash = "hash" });
        await ctx.SaveChangesAsync();
    }

    [Fact]
    public async Task RecordConsentAsync_PersistsConsentRecord()
    {
        var ctx = _dbFactory.CreateContext();
        var tenantId = Guid.NewGuid();
        var clientId = Guid.NewGuid();
        await SeedTenantAndClientAsync(ctx, tenantId, clientId);

        var sut = new ConsentService(ctx, _loggerMock.Object);
        var result = await sut.RecordConsentAsync(tenantId, clientId, "Marketing", granted: true, "1.2.3.4");

        result.Should().BeTrue();
        ctx.ChangeTracker.Clear();
        ctx.GdprConsents.Should().HaveCount(1);
        ctx.GdprConsents.First().IsGranted.Should().BeTrue();
        ctx.GdprConsents.First().ConsentType.Should().Be("Marketing");
    }

    [Fact]
    public async Task GetConsentStatusAsync_WhenGranted_ReturnsGranted()
    {
        var ctx = _dbFactory.CreateContext();
        var tenantId = Guid.NewGuid();
        var clientId = Guid.NewGuid();
        await SeedTenantAndClientAsync(ctx, tenantId, clientId);

        var sut = new ConsentService(ctx, _loggerMock.Object);
        await sut.RecordConsentAsync(tenantId, clientId, "Marketing", granted: true);

        var status = await sut.GetConsentStatusAsync(tenantId, clientId, "Marketing");

        status.Should().Be(ConsentStatus.Granted);
    }

    [Fact]
    public async Task GetConsentStatusAsync_WhenNoRecord_ReturnsNotRecorded()
    {
        var ctx = _dbFactory.CreateContext();
        var sut = new ConsentService(ctx, _loggerMock.Object);

        var status = await sut.GetConsentStatusAsync(Guid.NewGuid(), Guid.NewGuid(), "Marketing");

        status.Should().Be(ConsentStatus.NotRecorded);
    }

    [Fact]
    public async Task RevokeConsentAsync_AddsRevocationEntry()
    {
        var ctx = _dbFactory.CreateContext();
        var tenantId = Guid.NewGuid();
        var clientId = Guid.NewGuid();
        await SeedTenantAndClientAsync(ctx, tenantId, clientId);

        var sut = new ConsentService(ctx, _loggerMock.Object);
        await sut.RecordConsentAsync(tenantId, clientId, "Marketing", granted: true);
        await sut.RevokeConsentAsync(tenantId, clientId, "Marketing");

        // Latest entry should be revoked
        var status = await sut.GetConsentStatusAsync(tenantId, clientId, "Marketing");
        status.Should().Be(ConsentStatus.Revoked);
    }

    [Fact]
    public async Task GetAllConsentsAsync_ReturnsAllRecordsForClient()
    {
        var ctx = _dbFactory.CreateContext();
        var tenantId = Guid.NewGuid();
        var clientId = Guid.NewGuid();
        await SeedTenantAndClientAsync(ctx, tenantId, clientId);

        var otherTenantId = Guid.NewGuid();
        var otherClientId = Guid.NewGuid();
        await SeedTenantAndClientAsync(ctx, otherTenantId, otherClientId);

        var sut = new ConsentService(ctx, _loggerMock.Object);
        await sut.RecordConsentAsync(tenantId, clientId, "Marketing", true);
        await sut.RecordConsentAsync(tenantId, clientId, "Analytics", false);
        await sut.RecordConsentAsync(otherTenantId, otherClientId, "Marketing", true); // different client

        var consents = await sut.GetAllConsentsAsync(tenantId, clientId);

        consents.Should().HaveCount(2);
    }

    [Fact]
    public async Task AcceptDpaAsync_WhenNotPreviouslyAccepted_CreatesRecord()
    {
        var ctx = _dbFactory.CreateContext();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await SeedTenantAndUserAsync(ctx, tenantId, userId);

        var sut = new ConsentService(ctx, _loggerMock.Object);
        var result = await sut.AcceptDpaAsync(tenantId, userId, "v2.0");

        result.Should().BeTrue();
        ctx.ChangeTracker.Clear();
        ctx.LegalAgreements.Should().HaveCount(1);
        ctx.LegalAgreements.First().Version.Should().Be("v2.0");
    }

    [Fact]
    public async Task AcceptDpaAsync_WhenAlreadyAccepted_ReturnsTrue_WithoutDuplicate()
    {
        var ctx = _dbFactory.CreateContext();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await SeedTenantAndUserAsync(ctx, tenantId, userId);

        var sut = new ConsentService(ctx, _loggerMock.Object);
        await sut.AcceptDpaAsync(tenantId, userId, "v2.0");
        var result = await sut.AcceptDpaAsync(tenantId, userId, "v2.0");

        result.Should().BeTrue();
        ctx.ChangeTracker.Clear();
        ctx.LegalAgreements.Should().HaveCount(1); // No duplicate
    }
}
