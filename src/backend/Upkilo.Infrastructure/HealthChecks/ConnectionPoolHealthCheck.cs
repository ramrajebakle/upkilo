using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Upkilo.Infrastructure.HealthChecks;

/// <summary>
/// Health check that monitors database connection pool utilization
/// by querying PostgreSQL pg_stat_activity for real-time connection stats.
/// </summary>
public class ConnectionPoolHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ConnectionPoolHealthCheck> _logger;

    public ConnectionPoolHealthCheck(
        IConfiguration configuration,
        ILogger<ConnectionPoolHealthCheck> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
                return HealthCheckResult.Degraded("No connection string configured");

            var data = new Dictionary<string, object>();

            using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            data["connection_state"] = connection.State.ToString();
            data["server_version"] = connection.ServerVersion;

            // Query PostgreSQL for active connection stats
            using var cmd = new NpgsqlCommand(
                @"SELECT 
                    count(*) FILTER (WHERE state = 'active') as active,
                    count(*) FILTER (WHERE state = 'idle') as idle,
                    count(*) FILTER (WHERE state = 'idle in transaction') as idle_in_transaction,
                    count(*) as total,
                    (SELECT setting::int FROM pg_settings WHERE name = 'max_connections') as max_connections
                  FROM pg_stat_activity 
                  WHERE datname = current_database()", connection);

            using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                var active = reader.GetInt64(0);
                var idle = reader.GetInt64(1);
                var idleInTransaction = reader.GetInt64(2);
                var total = reader.GetInt64(3);
                var maxConnections = reader.GetInt32(4);

                data["active_connections"] = active;
                data["idle_connections"] = idle;
                data["idle_in_transaction"] = idleInTransaction;
                data["total_connections"] = total;
                data["max_connections"] = maxConnections;

                var utilizationPct = maxConnections > 0
                    ? (double)total / maxConnections * 100
                    : 0;
                data["utilization_pct"] = Math.Round(utilizationPct, 1);

                var maxPoolSize = _configuration.GetValue("Database:MaxPoolSize", 100);
                data["configured_pool_size"] = maxPoolSize;

                // Alert thresholds
                if (utilizationPct > 90)
                {
                    _logger.LogWarning("DB connection utilization at {Pct}% ({Total}/{Max})",
                        utilizationPct, total, maxConnections);
                    return HealthCheckResult.Degraded(
                        $"Connection utilization at {utilizationPct:F1}% — near capacity",
                        data: data);
                }

                if (idleInTransaction > 5)
                {
                    _logger.LogWarning("Found {Count} idle-in-transaction connections", idleInTransaction);
                    return HealthCheckResult.Degraded(
                        $"{idleInTransaction} idle-in-transaction connections detected — possible connection leak",
                        data: data);
                }
            }

            await connection.CloseAsync();
            return HealthCheckResult.Healthy("Connection pool healthy", data: data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connection pool health check failed");
            return HealthCheckResult.Unhealthy("Failed to check connection pool", ex);
        }
    }
}
