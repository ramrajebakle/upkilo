using Hangfire.Client;
using Hangfire.Server;
using Microsoft.Extensions.Logging;
using System;
using Upkilo.Core.Interfaces;

namespace Upkilo.Infrastructure.Background;

/// <summary>
/// Hangfire filter that preserves TenantId and injects a CorrelationId across job boundaries.
/// TenantId is captured at enqueue time and restored before execution.
/// CorrelationId is generated per execution so all log lines for a job share one ID.
/// Uses ILogger.BeginScope so the CorrelationId flows into Serilog's log context
/// without requiring a direct Serilog reference from this project.
/// </summary>
public class TenantJobContextFilter : IClientFilter, IServerFilter
{
    private const string TenantIdKey = "TenantId";

    // Holds the IDisposable returned by ILogger.BeginScope so we can dispose it
    // after the job finishes — prevents the scope leaking into the next job on this thread.
    [ThreadStatic]
    private static IDisposable? _loggerScope;

    private readonly ITenantContextAccessor _tenantContextAccessor;
    private readonly ILogger<TenantJobContextFilter> _logger;

    public TenantJobContextFilter(ITenantContextAccessor tenantContextAccessor, ILogger<TenantJobContextFilter> logger)
    {
        _tenantContextAccessor = tenantContextAccessor;
        _logger = logger;
    }

    public void OnCreating(CreatingContext filterContext)
    {
        var tenantId = _tenantContextAccessor.TenantId;
        if (tenantId.HasValue)
        {
            filterContext.SetJobParameter(TenantIdKey, tenantId.Value.ToString());
        }
    }

    public void OnCreated(CreatedContext filterContext) { }

    public void OnPerforming(PerformingContext filterContext)
    {
        var tenantIdStr = filterContext.GetJobParameter<string>(TenantIdKey);
        if (!string.IsNullOrEmpty(tenantIdStr) && Guid.TryParse(tenantIdStr, out var tenantId))
        {
            _tenantContextAccessor.SetContext(tenantId);
        }

        // Short unique ID for this job execution — groups all log lines emitted
        // during the job in Grafana/Application Insights queries.
        var correlationId = $"job-{Guid.NewGuid():N}"[..16];
        _loggerScope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["JobId"] = filterContext.BackgroundJob.Id
        });
    }

    public void OnPerformed(PerformedContext filterContext)
    {
        _tenantContextAccessor.Clear();
        _loggerScope?.Dispose();
        _loggerScope = null;
    }
}

