using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Upkilo.Core.Interfaces;

namespace Upkilo.Infrastructure.Data;

/// <summary>
/// EF Core Interceptor to automatically switch to Read-Only Replica for SELECT queries.
/// </summary>
public class ReadWriteInterceptor : DbCommandInterceptor
{
    private readonly IDbConnectionSelector _connectionSelector;

    public ReadWriteInterceptor(IDbConnectionSelector connectionSelector)
    {
        _connectionSelector = connectionSelector;
    }

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        UpdateConnection(command);
        return base.ReaderExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        UpdateConnection(command);
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    private void UpdateConnection(DbCommand command)
    {
        // If it's a SELECT query and not part of an explicit transaction, use replica
        if (command.CommandText.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) &&
            command.Transaction == null)
        {
            _connectionSelector.UseReplica(true);
        }
        else
        {
            _connectionSelector.UseReplica(false);
        }

        var newConnectionString = _connectionSelector.GetConnectionString();
        if (command.Connection != null && command.Connection.ConnectionString != newConnectionString)
        {
            if (command.Connection.State == System.Data.ConnectionState.Closed)
            {
                command.Connection.ConnectionString = newConnectionString;
            }
        }
    }
}
