using System;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Interfaces;

namespace Upkilo.Infrastructure.Data;

/// <summary>
/// Singleton monitor that tracks the health of the primary database globally.
/// </summary>
public class DatabaseHealthMonitor
{
    private readonly ILogger<DatabaseHealthMonitor> _logger;
    private readonly IBusinessMetrics _metrics;
    private readonly object _stateLock = new();
    private bool _isPrimaryDown;
    private DateTime? _lastDownTime;

    public DatabaseHealthMonitor(ILogger<DatabaseHealthMonitor> logger, IBusinessMetrics metrics)
    {
        _logger = logger;
        _metrics = metrics;
    }

    public bool IsPrimaryDown
    {
        get
        {
            lock (_stateLock)
            {
                if (_isPrimaryDown && _lastDownTime.HasValue &&
                    DateTime.UtcNow - _lastDownTime.Value > TimeSpan.FromSeconds(60))
                {
                    return false;
                }
                return _isPrimaryDown;
            }
        }
    }

    public void ReportFailure()
    {
        lock (_stateLock)
        {
            if (!_isPrimaryDown)
            {
                _logger.LogCritical("GLOBAL FAILOVER: Primary database reported failure. Switching all traffic to replica.");
                _isPrimaryDown = true;
                _lastDownTime = DateTime.UtcNow;
                _metrics.RecordDatabaseFailover();
            }
        }
    }

    public void ReportSuccess()
    {
        lock (_stateLock)
        {
            if (_isPrimaryDown)
            {
                _logger.LogInformation("GLOBAL RECOVERY: Primary database is back online. Resuming normal operations.");
                _isPrimaryDown = false;
                _lastDownTime = null;
            }
        }
    }
}
