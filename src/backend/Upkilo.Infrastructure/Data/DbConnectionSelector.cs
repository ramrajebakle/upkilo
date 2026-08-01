using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Distributed;
using Upkilo.Core.Interfaces;
using Polly;
using Polly.Registry;

namespace Upkilo.Infrastructure.Data;

/// <summary>
/// Managed database connection selection for Read/Write splitting and Failover.
/// Incorporates Polly v8 resilience pipelines for connection robustness.
/// </summary>
public class DbConnectionSelector : IDbConnectionSelector
{
    private readonly string _primaryConnection;
    private readonly string _replicaConnection;
    private readonly ILogger<DbConnectionSelector> _logger;
    private readonly DatabaseHealthMonitor _healthMonitor;
    private readonly ITenantProvider _tenantProvider;
    private readonly IDistributedCache _cache;
    private readonly IBusinessMetrics _metrics;
    private readonly ResiliencePipeline _resiliencePipeline;
    private bool _useReplica;

    public DbConnectionSelector(
        IConfiguration configuration,
        ILogger<DbConnectionSelector> logger,
        DatabaseHealthMonitor healthMonitor,
        ITenantProvider tenantProvider,
        IDistributedCache cache,
        ResiliencePipelineRegistry<string> pipelineRegistry,
        IBusinessMetrics metrics)
    {
        _primaryConnection = configuration.GetConnectionString("DefaultConnection")
            ?? configuration["Database:PrimaryConnectionString"]
            ?? throw new InvalidOperationException(
                "Database connection string is not configured. Set ConnectionStrings:DefaultConnection.");
        _replicaConnection = configuration.GetConnectionString("ReplicaConnection")
            ?? configuration["Database:ReplicaConnectionString"]
            ?? _primaryConnection;
        _logger = logger;
        _healthMonitor = healthMonitor;
        _tenantProvider = tenantProvider;
        _cache = cache;
        _metrics = metrics;

        // Use the "default" resilience pipeline registered in Program.cs
        _resiliencePipeline = pipelineRegistry.GetPipeline("default");
    }

    public string GetConnectionString()
    {
        // 1. Dynamic Tenant Isolation (PRO/Enterprise)
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId.HasValue)
        {
            // NOTE: IDistributedCache.GetString internally does GetAsync().GetAwaiter().GetResult().
            // This is a known sync-over-async issue tracked for resolution when the interface supports async.
            var tenantConn = _cache.GetString($"tenant_db:{tenantId}");
            if (!string.IsNullOrEmpty(tenantConn))
            {
                return tenantConn;
            }
        }

        // 2. Global Failover logic
        if (_healthMonitor.IsPrimaryDown)
        {
            _logger.LogWarning("PRIMARY database is DOWN. Routing all traffic to REPLICA.");
            return _replicaConnection;
        }

        // 3. Per-request Read/Write splitting logic
        var connectionString = _useReplica ? _replicaConnection : _primaryConnection;

        if (_useReplica)
        {
            _logger.LogDebug("Routing request to REPLICA database.");
        }

        return connectionString;
    }

    public void UseReplica(bool useReplica = true)
    {
        _useReplica = useReplica;
    }

    public void MarkPrimaryDown(bool isDown = true)
    {
        if (isDown)
        {
            if (!_healthMonitor.IsPrimaryDown)
            {
                _metrics.RecordDatabaseFailover();
            }
            _healthMonitor.ReportFailure();
        }
        else
        {
            _healthMonitor.ReportSuccess();
        }
    }

    /// <summary>
    /// Executes a database action within the resilience pipeline.
    /// </summary>
    public async Task<T> ExecuteResilientlyAsync<T>(Func<Task<T>> action)
    {
        return await _resiliencePipeline.ExecuteAsync(async _ => await action());
    }
}
