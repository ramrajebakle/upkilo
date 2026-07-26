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

public class FinancialProjectionServiceTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory;
    private readonly Mock<ILogger<FinancialProjectionService>> _loggerMock = new();

    public FinancialProjectionServiceTests() => _dbFactory = new TestDbContextFactory();
    public void Dispose() => _dbFactory.Dispose();

    private (FinancialProjectionService sut, Upkilo.Infrastructure.Data.AppDbContext ctx, Guid tenantId) CreateSut()
    {
        var ctx = _dbFactory.CreateContext();
        var tenantId = Guid.NewGuid();
        ctx.Tenants.Add(new Tenant { Id = tenantId, Name = "T", Slug = "t" });
        ctx.SaveChanges();
        return (new FinancialProjectionService(ctx, _loggerMock.Object), ctx, tenantId);
    }

    [Fact]
    public async Task PredictRevenueAsync_WithNoPaidInvoices_ReturnsZero()
    {
        var (sut, _, tenantId) = CreateSut();

        var result = await sut.PredictRevenueAsync(tenantId, monthsAhead: 3);

        result.Should().Be(0m);
    }

    [Fact]
    public async Task PredictRevenueAsync_WithPaidInvoices_ReturnsPositiveProjection()
    {
        var (sut, ctx, tenantId) = CreateSut();

        // Seed 6 paid invoices ($100 each, within last 6 months)
        for (int i = 1; i <= 6; i++)
        {
            ctx.Invoices.Add(new Invoice
            {
                Id = Guid.NewGuid(), TenantId = tenantId,
                IssuedAt = DateTime.UtcNow.AddMonths(-i).AddDays(1),
                TotalAmount = 100m, Status = InvoiceStatus.Paid,
                InvoiceNumber = $"INV-{i:000}", DueDate = DateTime.UtcNow.AddMonths(-i + 1)
            });
        }
        await ctx.SaveChangesAsync();

        var result = await sut.PredictRevenueAsync(tenantId, monthsAhead: 3);

        // 6 months * $100 = $600 total → avg $100/month → 3 months = $300
        result.Should().Be(300m);
    }

    [Fact]
    public async Task GetCashflowForecastAsync_Returns30ForecastPoints()
    {
        var (sut, _, tenantId) = CreateSut();

        var forecast = await sut.GetCashflowForecastAsync(tenantId);

        forecast.Should().NotBeNull();
        forecast.ForecastPoints.Should().HaveCount(30);
    }

    [Fact]
    public async Task GetCashflowForecastAsync_AllPointsHavePositiveOrZeroRevenue()
    {
        var (sut, _, tenantId) = CreateSut();

        var forecast = await sut.GetCashflowForecastAsync(tenantId);

        forecast.ForecastPoints.Should().AllSatisfy(p => p.ProjectedRevenue.Should().BeGreaterOrEqualTo(0));
    }

    [Fact]
    public async Task PredictChurnRiskAsync_WithNoData_ReturnsEmpty()
    {
        var (sut, _, tenantId) = CreateSut();

        var risks = await sut.PredictChurnRiskAsync(tenantId);

        risks.Should().BeEmpty();
    }

    [Fact]
    public async Task GenerateTaxReportAsync_WithNoPaidBookings_ReturnsZeroRevenue()
    {
        var (sut, _, tenantId) = CreateSut();
        var start = DateTime.UtcNow.AddMonths(-1);
        var end = DateTime.UtcNow;

        var report = await sut.GenerateTaxReportAsync(tenantId, start, end);

        report.TotalRevenue.Should().Be(0);
        report.TaxLiability.Should().Be(0);
    }
}
