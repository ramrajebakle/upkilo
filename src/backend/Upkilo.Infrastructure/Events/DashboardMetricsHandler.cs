using MediatR;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Events;
using Upkilo.Infrastructure.Data;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Upkilo.Infrastructure.Events;

public class DashboardMetricsHandler : 
    INotificationHandler<BookingCreated>,
    INotificationHandler<BookingConfirmed>,
    INotificationHandler<BookingCancelled>,
    INotificationHandler<BookingCompleted>,
    INotificationHandler<PaymentReceived>,
    INotificationHandler<ClientCreated>
{
    private readonly AppDbContext _context;

    public DashboardMetricsHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task Handle(BookingCreated notification, CancellationToken cancellationToken)
    {
        var stats = await GetOrAddStats(notification.TenantId, cancellationToken);
        stats.TotalBookings++;
        stats.PendingBookings++;
        
        if (notification.StartTime.Month == DateTime.UtcNow.Month && notification.StartTime.Year == DateTime.UtcNow.Year)
        {
            stats.BookingsThisMonth++;
        }

        var daily = await GetOrAddDailyMetric(notification.TenantId, notification.StartTime.Date, cancellationToken);
        daily.BookingCount++;

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task Handle(BookingConfirmed notification, CancellationToken cancellationToken)
    {
        // No major stat change on confirm yet, maybe stats.ConfirmedBookings++
        await Task.CompletedTask;
    }

    public async Task Handle(BookingCancelled notification, CancellationToken cancellationToken)
    {
        var stats = await GetOrAddStats(notification.TenantId, cancellationToken);
        stats.PendingBookings = Math.Max(0, stats.PendingBookings - 1);

        var daily = await GetOrAddDailyMetric(notification.TenantId, DateTime.UtcNow.Date, cancellationToken);
        daily.CancelledBookingCount++;

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task Handle(BookingCompleted notification, CancellationToken cancellationToken)
    {
        var stats = await GetOrAddStats(notification.TenantId, cancellationToken);
        stats.PendingBookings = Math.Max(0, stats.PendingBookings - 1);
        stats.CompletedBookings++;

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task Handle(PaymentReceived notification, CancellationToken cancellationToken)
    {
        var stats = await GetOrAddStats(notification.TenantId, cancellationToken);
        stats.TotalRevenue += notification.Amount;
        
        if (notification.OccurredAt.Month == DateTime.UtcNow.Month && notification.OccurredAt.Year == DateTime.UtcNow.Year)
        {
            stats.RevenueThisMonth += notification.Amount;
        }

        var daily = await GetOrAddDailyMetric(notification.TenantId, notification.OccurredAt.Date, cancellationToken);
        daily.Revenue += notification.Amount;

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task Handle(ClientCreated notification, CancellationToken cancellationToken)
    {
        var stats = await GetOrAddStats(notification.TenantId, cancellationToken);
        stats.TotalClients++;

        var daily = await GetOrAddDailyMetric(notification.TenantId, notification.OccurredAt.Date, cancellationToken);
        daily.NewClientCount++;

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<TenantDashboardStats> GetOrAddStats(Guid tenantId, CancellationToken ct)
    {
        var stats = await _context.TenantDashboardStats
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.TenantId == tenantId, ct);

        if (stats == null)
        {
            stats = new TenantDashboardStats { TenantId = tenantId };
            _context.TenantDashboardStats.Add(stats);
        }

        return stats;
    }

    private async Task<TenantDailyMetric> GetOrAddDailyMetric(Guid tenantId, DateTime date, CancellationToken ct)
    {
        var metric = await _context.TenantDailyMetrics
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.TenantId == tenantId && m.Date == date, ct);

        if (metric == null)
        {
            metric = new TenantDailyMetric { TenantId = tenantId, Date = date };
            _context.TenantDailyMetrics.Add(metric);
        }

        return metric;
    }
}
