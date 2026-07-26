using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Prometheus;
using System.Data.Common;

namespace Upkilo.Infrastructure.Data;

/// <summary>
/// EF Core interceptor that detects potentially slow queries.
/// Logs a warning when any query takes longer than 500ms.
/// Helps identify N+1 problems and missing indexes before they reach production.
/// </summary>
public class SlowQueryInterceptor : DbCommandInterceptor
{
    private readonly ILogger<SlowQueryInterceptor> _logger;
    private const int SlowQueryThresholdMs = 500;

    // Prometheus counter scraped by the /metrics endpoint.
    // Alert on upkilo_slow_queries_total > N over 5m to page on-call before users feel it.
    private static readonly Counter SlowQueryCounter = Metrics.CreateCounter(
        "upkilo_slow_queries_total",
        "Total number of database queries exceeding the slow-query threshold.",
        labelNames: ["command_type"]);

    public SlowQueryInterceptor(ILogger<SlowQueryInterceptor> logger)
    {
        _logger = logger;
    }

    public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
    {
        LogSlowQuery(command, eventData);
        return base.ReaderExecuted(command, eventData, result);
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
    {
        LogSlowQuery(command, eventData);
        return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result)
    {
        LogSlowQuery(command, eventData);
        return base.NonQueryExecuted(command, eventData, result);
    }

    public override object? ScalarExecuted(DbCommand command, CommandExecutedEventData eventData, object? result)
    {
        LogSlowQuery(command, eventData);
        return base.ScalarExecuted(command, eventData, result);
    }

    private void LogSlowQuery(DbCommand command, CommandExecutedEventData eventData)
    {
        if (eventData.Duration.TotalMilliseconds > SlowQueryThresholdMs)
        {
            var commandType = command.CommandType == System.Data.CommandType.StoredProcedure
                ? "stored_procedure"
                : "sql";

            SlowQueryCounter.WithLabels(commandType).Inc();

            // Log parameter NAMES and TYPES only — never values, which can contain
            // PII (emails, phone numbers, hashed passwords passed to WHERE clauses).
            var paramSummary = string.Join(", ", command.Parameters.Cast<DbParameter>()
                .Select(p => $"{p.ParameterName}:{p.DbType}"));

            _logger.LogWarning(
                "SLOW QUERY {DurationMs}ms — SQL: {CommandText} — Params: {Parameters}",
                (int)eventData.Duration.TotalMilliseconds,
                command.CommandText.Length > 1000 ? command.CommandText[..1000] + "..." : command.CommandText,
                paramSummary);
        }
    }
}
