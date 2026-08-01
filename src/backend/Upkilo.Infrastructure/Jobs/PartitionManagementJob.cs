using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Upkilo.Infrastructure.Data;
using Npgsql;

namespace Upkilo.Infrastructure.Jobs;

/// <summary>
/// Hangfire job to automatically manage PostgreSQL range partitions for high-volume tables.
/// Ensures that partitions for the next 3 months always exist.
/// </summary>
public class PartitionManagementJob
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<PartitionManagementJob> _logger;

    public PartitionManagementJob(AppDbContext dbContext, ILogger<PartitionManagementJob> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        _logger.LogInformation("Starting PostgreSQL partition management job.");

        try
        {
            // Manage partitions for AuditEntries (Timestamp)
            await ManageMonthlyRangePartitionsAsync("AuditEntries", "Timestamp");

            // Manage partitions for Bookings (StartTime)
            await ManageMonthlyRangePartitionsAsync("Bookings", "StartTime");

            _logger.LogInformation("Successfully completed partition management.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to manage PostgreSQL partitions.");
            throw;
        }
    }

    private async Task ManageMonthlyRangePartitionsAsync(string tableName, string columnName)
    {
        var now = DateTime.UtcNow;

        // Ensure partitions exist for the current month and the next 3 months
        for (int i = 0; i <= 3; i++)
        {
            var date = now.AddMonths(i);
            var year = date.Year;
            var month = date.Month;

            var partitionName = $"{tableName.ToLower()}_y{year}m{month:D2}";
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1);

            await CreatePartitionIfNotExistsAsync(tableName, partitionName, startDate, endDate);
        }
    }

    // SECURITY: only these tables are managed by this job — prevents SQL injection via masterTable
    private static readonly HashSet<string> AllowedTables = new(StringComparer.OrdinalIgnoreCase)
    {
        "AuditEntries", "Bookings"
    };

    private async Task CreatePartitionIfNotExistsAsync(string masterTable, string partitionName, DateTime start, DateTime end)
    {
        if (!AllowedTables.Contains(masterTable))
            throw new ArgumentException($"Table '{masterTable}' is not in the allowed partition management list.", nameof(masterTable));

        // Validate partitionName to only alphanumeric + underscore to prevent injection
        if (!System.Text.RegularExpressions.Regex.IsMatch(partitionName, @"^[a-z0-9_]+$"))
            throw new ArgumentException($"Partition name '{partitionName}' contains invalid characters.", nameof(partitionName));

        // masterTable is safe (validated above); partitionName is validated above.
        // Dates are formatted via ToString and are not user-supplied.
        var sql = $@"
            DO $$
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace WHERE c.relname = '{partitionName}') THEN
                    CREATE TABLE {partitionName} PARTITION OF ""{masterTable}""
                    FOR VALUES FROM ('{start:yyyy-MM-dd}') TO ('{end:yyyy-MM-dd}');
                END IF;
            END $$;";

        try
        {
            _dbContext.Database.SetCommandTimeout(TimeSpan.FromSeconds(60));
            await _dbContext.Database.ExecuteSqlRawAsync(sql);
            _logger.LogDebug("Verified partition {PartitionName} for {MasterTable}", partitionName, masterTable);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating partition {PartitionName} for {MasterTable}. Future INSERTs into this range will fail.", partitionName, masterTable);
            throw;
        }
    }
}
