using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Interfaces;

namespace Upkilo.Infrastructure.Services;

public class SystemLoadMonitorService : ISystemLoadMonitorService
{
    private readonly ILogger<SystemLoadMonitorService> _logger;
    private readonly int _degradedQueueThreshold;
    private readonly int _overloadedQueueThreshold;
    private readonly long _degradedMemoryMb;
    private readonly long _overloadedMemoryMb;

    public SystemLoadMonitorService(ILogger<SystemLoadMonitorService> logger, IConfiguration configuration)
    {
        _logger = logger;
        var section = configuration.GetSection("SystemLoadMonitor");
        _degradedQueueThreshold = section.GetValue("DegradedThreshold", 1000);
        _overloadedQueueThreshold = section.GetValue("OverloadedThreshold", 5000);
        _degradedMemoryMb = section.GetValue("MemoryDegradedMb", 512L);
        _overloadedMemoryMb = section.GetValue("MemoryOverloadedMb", 768L);
    }

    public bool IsSystemDegraded()
    {
        try
        {
            if (JobStorage.Current == null) return false;
            var monitoringApi = JobStorage.Current.GetMonitoringApi();
            long enqueued = monitoringApi.EnqueuedCount("default");
            long memoryMb = System.Diagnostics.Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024;

            return enqueued > _degradedQueueThreshold || memoryMb > _degradedMemoryMb;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check system load. Defaulting to non-degraded.");
            return false;
        }
    }

    public bool IsSystemOverloaded()
    {
        try
        {
            if (JobStorage.Current == null) return false;
            var monitoringApi = JobStorage.Current.GetMonitoringApi();
            long enqueued = monitoringApi.EnqueuedCount("default");
            long memoryMb = System.Diagnostics.Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024;

            return enqueued > _overloadedQueueThreshold || memoryMb > _overloadedMemoryMb;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check system load. Defaulting to non-overloaded.");
            return false;
        }
    }
}
