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

public class WaiverPdfServiceTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory;
    private readonly Mock<ILogger<WaiverPdfService>> _loggerMock;

    public WaiverPdfServiceTests()
    {
        _dbFactory = new TestDbContextFactory();
        _loggerMock = new Mock<ILogger<WaiverPdfService>>();
    }

    [Fact]
    public void Constructor_ValidArgs_CreatesInstance()
    {
        using var ctx = _dbFactory.CreateContext();
        var svc = new WaiverPdfService(ctx, _loggerMock.Object);
        svc.Should().NotBeNull();
    }

    [Fact]
    public async Task GenerateWaiverPdfAsync_UnknownSignatureId_ThrowsInvalidOperation()
    {
        using var ctx = _dbFactory.CreateContext();
        var svc = new WaiverPdfService(ctx, _loggerMock.Object);

        var tenantId = Guid.NewGuid();
        var unknownSignatureId = Guid.NewGuid();

        var act = async () => await svc.GenerateWaiverPdfAsync(tenantId, unknownSignatureId);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GenerateWaiverPdfAsync_KnownSignature_ReturnsByteArray()
    {
        using var ctx = _dbFactory.CreateContext();

        var tenantId = Guid.NewGuid();
        var clientId = Guid.NewGuid();
        var waiverId = Guid.NewGuid();
        var signatureId = Guid.NewGuid();

        ctx.Tenants.Add(new Tenant { Id = tenantId, Name = "Test Biz", Slug = "test-biz" });

        var client = new Client
        {
            Id = clientId,
            TenantId = tenantId,
            FirstName = "Jane",
            LastName = "Doe",
            Email = "jane@example.com"
        };
        ctx.Clients.Add(client);

        var waiver = new DigitalWaiver
        {
            Id = waiverId,
            TenantId = tenantId,
            Title = "Test Waiver",
            Content = "I agree to the terms."
        };
        ctx.Set<DigitalWaiver>().Add(waiver);

        var signature = new WaiverSignature
        {
            Id = signatureId,
            WaiverId = waiverId,
            ClientId = clientId,
            SignedAt = DateTime.UtcNow,
            SignedFromIP = "127.0.0.1"
        };
        ctx.Set<WaiverSignature>().Add(signature);
        ctx.SaveChanges();

        var svc = new WaiverPdfService(ctx, _loggerMock.Object);
        var result = await svc.GenerateWaiverPdfAsync(tenantId, signatureId);

        result.Should().NotBeNull();
        result.Length.Should().BeGreaterThan(0);
    }

    public void Dispose() => _dbFactory.Dispose();
}
