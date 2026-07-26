using System;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Services;
using Upkilo.Tests.Helpers;
using Xunit;

namespace Upkilo.Tests.Services;

public class ExportServiceTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory;

    public ExportServiceTests()
    {
        _dbFactory = new TestDbContextFactory();
    }

    [Fact]
    public void Constructor_ValidArgs_CreatesInstance()
    {
        using var ctx = _dbFactory.CreateContext();
        var svc = new ExportService(ctx);
        svc.Should().NotBeNull();
    }

    [Fact]
    public async Task ExportClientsToCsvAsync_NoClients_ReturnsCsvHeader()
    {
        using var ctx = _dbFactory.CreateContext();
        var svc = new ExportService(ctx);

        var result = await svc.ExportClientsToCsvAsync(Guid.NewGuid());

        result.Should().NotBeNull();
        result.Length.Should().BeGreaterThan(0);
        var csv = Encoding.UTF8.GetString(result);
        csv.Should().Contain("FirstName");
        csv.Should().Contain("LastName");
        csv.Should().Contain("Email");
    }

    [Fact]
    public async Task ExportBookingsToCsvAsync_NoBookings_ReturnsCsvHeader()
    {
        using var ctx = _dbFactory.CreateContext();
        var svc = new ExportService(ctx);

        var result = await svc.ExportBookingsToCsvAsync(Guid.NewGuid());

        result.Should().NotBeNull();
        result.Length.Should().BeGreaterThan(0);
        var csv = Encoding.UTF8.GetString(result);
        csv.Should().Contain("ClientName");
        csv.Should().Contain("Status");
    }

    [Fact]
    public async Task ExportClientsToCsvAsync_WithClients_IncludesClientData()
    {
        using var ctx = _dbFactory.CreateContext();
        var tenantId = Guid.NewGuid();

        ctx.Tenants.Add(new Tenant { Id = tenantId, Name = "Test", Slug = "export-test" });
        ctx.Clients.Add(new Client
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FirstName = "John",
            LastName = "Export",
            Email = "john@export.com"
        });
        ctx.SaveChanges();

        var svc = new ExportService(ctx);
        var result = await svc.ExportClientsToCsvAsync(tenantId);

        var csv = Encoding.UTF8.GetString(result);
        csv.Should().Contain("John");
        csv.Should().Contain("Export");
    }

    public void Dispose() => _dbFactory.Dispose();
}
