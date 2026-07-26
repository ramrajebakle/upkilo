using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// Database migration rollback strategy and execution
/// </summary>
public class MigrationRollbackService
{
    private readonly AppDbContext _context;
    private readonly ILogger<MigrationRollbackService> _logger;

    public MigrationRollbackService(AppDbContext context, ILogger<MigrationRollbackService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Gets the list of applied migrations
    /// </summary>
    public async Task<IReadOnlyList<string>> GetAppliedMigrationsAsync()
    {
        var migrations = await _context.Database.GetAppliedMigrationsAsync();
        return migrations.ToList().AsReadOnly();
    }

    /// <summary>
    /// Gets the list of pending migrations
    /// </summary>
    public async Task<IReadOnlyList<string>> GetPendingMigrationsAsync()
    {
        var migrations = await _context.Database.GetPendingMigrationsAsync();
        return migrations.ToList().AsReadOnly();
    }

    /// <summary>
    /// Creates a backup marker before running a migration (for rollback tracking)
    /// </summary>
    public async Task<string> CreateRollbackPointAsync(string migrationName)
    {
        var rollbackId = $"rb_{migrationName}_{DateTime.UtcNow:yyyyMMddHHmmss}";
        
        var record = new Upkilo.Core.Entities.MigrationRecord
        {
            MigrationId = migrationName,
            Version = "1.0",
            Description = $"Rollback point for {migrationName}",
            Script = "",
            RollbackScript = $"-- Rollback for {migrationName}\n-- Generated at {DateTime.UtcNow:O}",
            Status = "Applied",
            AppliedAt = DateTime.UtcNow
        };

        _context.MigrationRecords.Add(record);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Rollback point created: {RollbackId} for migration {Migration}", rollbackId, migrationName);
        return rollbackId;
    }

    /// <summary>
    /// Validates migration health after applying
    /// </summary>
    public async Task<MigrationHealthResult> ValidateMigrationHealthAsync()
    {
        try
        {
            var canConnect = await _context.Database.CanConnectAsync();
            var pending = await GetPendingMigrationsAsync();
            var applied = await GetAppliedMigrationsAsync();

            return new MigrationHealthResult
            {
                IsHealthy = canConnect && !pending.Any(),
                CanConnect = canConnect,
                AppliedCount = applied.Count,
                PendingCount = pending.Count,
                LatestMigration = applied.LastOrDefault() ?? "None"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Migration health check failed");
            return new MigrationHealthResult { IsHealthy = false, Error = ex.Message };
        }
    }
}

public class MigrationHealthResult
{
    public bool IsHealthy { get; set; }
    public bool CanConnect { get; set; }
    public int AppliedCount { get; set; }
    public int PendingCount { get; set; }
    public string LatestMigration { get; set; } = string.Empty;
    public string? Error { get; set; }
}
