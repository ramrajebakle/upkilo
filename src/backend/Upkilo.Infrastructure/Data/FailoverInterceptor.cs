using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Upkilo.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Upkilo.Infrastructure.Data;

/// <summary>
/// EF Core interceptor that detects database connection failures and triggers
/// the IDbConnectionSelector to failover to a replica.
/// </summary>
public class FailoverInterceptor : DbConnectionInterceptor
{
    private readonly IDbConnectionSelector _connectionSelector;
    private readonly ILogger<FailoverInterceptor> _logger;

    public FailoverInterceptor(IDbConnectionSelector connectionSelector, ILogger<FailoverInterceptor> logger)
    {
        _connectionSelector = connectionSelector;
        _logger = logger;
    }

    public override void ConnectionFailed(DbConnection connection, ConnectionErrorEventData eventData)
    {
        HandleConnectionFailure(eventData.Exception);
        base.ConnectionFailed(connection, eventData);
    }

    public override Task ConnectionFailedAsync(DbConnection connection, ConnectionErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        HandleConnectionFailure(eventData.Exception);
        return base.ConnectionFailedAsync(connection, eventData, cancellationToken);
    }

    // Consecutive failure counter — require 3 failures before triggering failover to avoid
    // false positives from transient single-query errors (e.g. lock timeout).
    private int _consecutiveFailures;
    private const int FailoverThreshold = 3;

    private void HandleConnectionFailure(Exception exception)
    {
        if (IsTransientConnectionFailure(exception))
        {
            var count = System.Threading.Interlocked.Increment(ref _consecutiveFailures);
            _logger.LogWarning(exception, "Database connection failed ({Count}/{Threshold}). Will failover at threshold.", count, FailoverThreshold);
            if (count >= FailoverThreshold)
            {
                _logger.LogError("Failover threshold reached. Triggering replica failover.");
                _connectionSelector.MarkPrimaryDown(true);
            }
        }
        else
        {
            System.Threading.Interlocked.Exchange(ref _consecutiveFailures, 0);
        }
    }

    private static bool IsTransientConnectionFailure(Exception exception)
    {
        if (exception is NpgsqlException npgsqlEx)
        {
            return npgsqlEx.SqlState == "08001" || npgsqlEx.SqlState == "08006" ||
                   npgsqlEx.SqlState == "57P03" || npgsqlEx.SqlState == "57P01" ||
                   npgsqlEx.InnerException is System.Net.Sockets.SocketException or
                                              System.IO.IOException or
                                              TimeoutException;
        }
        return exception is System.Net.Sockets.SocketException or
                           System.IO.IOException or
                           TimeoutException;
    }
}
