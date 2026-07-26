using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// Implementation of IReportingService for dynamic analytics and funnel visualizations.
/// </summary>
public class ReportingService : IReportingService
{
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<ReportingService> _logger;

    public ReportingService(AppDbContext context, IEmailService emailService, ILogger<ReportingService> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<ReportDataResult> ExecuteReportAsync(Guid tenantId, ReportDefinition definition, Dictionary<string, string>? runtimeFilters = null)
    {
        var result = new ReportDataResult();
        
        // 1. Data Retrieval based on ReportType
        IQueryable query = definition.ReportType.ToLower() switch
        {
            "sales" => _context.Invoices.Where(i => i.TenantId == tenantId && !i.IsDeleted),
            "bookings" => _context.Bookings.Where(b => b.TenantId == tenantId && !b.IsDeleted),
            "clients" => _context.Clients.Where(c => c.TenantId == tenantId && !c.IsDeleted),
            _ => throw new ArgumentException("Invalid report type", nameof(definition.ReportType))
        };

        // 2. Parse ConfigJson for dynamic columns/grouping
        var config = JsonSerializer.Deserialize<ReportConfig>(definition.ConfigJson) ?? new ReportConfig();
        
        // 3. Apply Runtime and Saved Filters
        if (config.Filters != null && config.Filters.Any())
        {
            foreach (var filter in config.Filters)
            {
                var combinedFilter = $"{filter.Key} {filter.Value}";
                ValidateFilter(combinedFilter);
                query = query.Where(combinedFilter);
            }
        }

        if (runtimeFilters != null && runtimeFilters.Any())
        {
            foreach (var filter in runtimeFilters)
            {
                ValidateFilter($"{filter.Key} == @0");
                query = query.Where(filter.Key + " == @0", filter.Value);
            }
        }

        // 4. Dynamic Projection
        var columns = config.Columns ?? GetDefaultColumns(definition.ReportType);
        result.Columns = columns;
        
        var selectClause = "new(" + string.Join(", ", columns.Select(c => c + " as " + c)) + ")";
        
        var items = await query.Select(selectClause).Take(1000).ToDynamicListAsync();

        result.Rows = items.Select(item => {
            var row = new Dictionary<string, object>();
            foreach (var col in columns)
            {
                var val = GetPropertyValue(item, col);
                row[col] = val ?? string.Empty;
            }
            return row;
        }).ToList();

        return result;
    }

    private void ValidateFilter(string filter)
    {
        if (string.IsNullOrWhiteSpace(filter)) return;

        // Prevention against Dynamic LINQ injection: White-list allowed properties
        var allowedProps = new[] { "Status", "TotalAmount", "InvoiceNumber", "StartTime", "ClientName", "ServiceName", "CreatedAt", "TenantId", "Email", "FirstName", "LastName" };
        
        foreach (var word in filter.Split(new[] { ' ', '(', ')', '=', '>', '<', '!' }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (word.Contains(".") || word.Contains("[")) continue; 
            
            if (char.IsLetter(word[0]))
            {
                if (!allowedProps.Any(p => p.Equals(word, StringComparison.OrdinalIgnoreCase)))
                {
                    _logger.LogError("Filtering by forbidden property detected: {Property}", word);
                    throw new UnauthorizedAccessException($"Filtering by property '{word}' is not permitted.");
                }
            }
        }
    }

    private List<string> GetDefaultColumns(string reportType)
    {
        return reportType.ToLower() switch
        {
            "sales" => new List<string> { "InvoiceNumber", "IssueDate", "TotalAmount", "Status" },
            "bookings" => new List<string> { "Id", "StartTime", "Status", "Price" },
            "clients" => new List<string> { "FirstName", "LastName", "Email", "Phone" },
            _ => new List<string> { "Id" }
        };
    }

    private static readonly ConcurrentDictionary<(Type, string), PropertyInfo?> _propCache = new();

    private static object? GetPropertyValue(object item, string propertyName)
    {
        var pi = _propCache.GetOrAdd(
            (item.GetType(), propertyName),
            key => key.Item1.GetProperty(key.Item2));
        return pi?.GetValue(item, null);
    }

    public async Task<FunnelResult> GetFunnelAnalyticsAsync(Guid tenantId, DateTime from, DateTime to)
    {
        var visitorCount = await _context.PageAnalyticsRecords.Where(p => p.TenantId == tenantId && p.Timestamp >= from && p.Timestamp <= to).SumAsync(p => p.TotalViews);

        var serviceViews = await _context.PageAnalyticsRecords
            .Where(p => p.TenantId == tenantId && p.Timestamp >= from && p.Timestamp <= to
                && (p.PageUrl.Contains("/services") || p.PageUrl.Contains("/book")))
            .SumAsync(p => p.TotalViews);

        var bookingConfirmed = await _context.Bookings.CountAsync(b => b.TenantId == tenantId && b.CreatedAt >= from && b.CreatedAt <= to);
        var paymentAttempts = await _context.Invoices.CountAsync(i => i.TenantId == tenantId && i.CreatedAt >= from && i.CreatedAt <= to && !i.IsDeleted);

        return new FunnelResult
        {
            Steps = new List<FunnelStepData>
            {
                new() { Name = "Website Visitors", Count = Math.Max(visitorCount, 0) },
                new() { Name = "Service View", Count = serviceViews > 0 ? serviceViews : 0 },
                new() { Name = "Pay/Checkout", Count = paymentAttempts },
                new() { Name = "Booking Confirmed", Count = bookingConfirmed }
            },
            OverallConversionRate = visitorCount > 0 ? (decimal)bookingConfirmed / visitorCount : 0
        };
    }

    public async Task ProcessScheduledReportsAsync()
    {
        // Process in pages of 100 to avoid loading all scheduled reports into memory at once.
        const int batchSize = 100;
        int offset = 0;

        while (true)
        {
            var batch = await _context.ReportDefinitions
                .Where(r => r.IsScheduled && !r.IsArchived
                    && (r.LastRunAt == null || r.LastRunAt < DateTime.UtcNow.AddHours(-1)))
                .OrderBy(r => r.Id)
                .Skip(offset)
                .Take(batchSize)
                .ToListAsync();

            if (batch.Count == 0) break;

            // Process reports in parallel (max 5 concurrent DB queries) then save once per batch.
            await Parallel.ForEachAsync(batch, new ParallelOptions { MaxDegreeOfParallelism = 5 }, async (report, _) =>
            {
                try
                {
                    var data = await ExecuteReportAsync(report.TenantId, report);

                    if (!string.IsNullOrEmpty(report.ScheduledEmailRecipients))
                    {
                        var recipients = report.ScheduledEmailRecipients.Split(',', StringSplitOptions.RemoveEmptyEntries);
                        foreach (var recipient in recipients)
                        {
                            await _emailService.SendEmailAsync(recipient, $"Scheduled Report: {report.Name}",
                                $"The report {report.Name} has been generated. Column count: {data.Columns.Count}, Row count: {data.Rows.Count}");
                        }
                    }

                    report.LastRunAt = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process scheduled report {ReportId}", report.Id);
                }
            });

            // One SaveChanges per batch instead of one per report.
            await _context.SaveChangesAsync();
            offset += batch.Count;
        }
    }
}

internal class ReportConfig
{
    public List<string>? Columns { get; set; }
    public Dictionary<string, string>? Filters { get; set; }
}
