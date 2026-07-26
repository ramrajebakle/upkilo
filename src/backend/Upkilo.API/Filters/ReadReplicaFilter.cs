using Microsoft.AspNetCore.Mvc.Filters;
using Upkilo.Core.Interfaces;

namespace Upkilo.API.Filters;

/// <summary>
/// SC1: Action filter that routes the current request to the PostgreSQL read replica.
/// Applied to AnalyticsController, ReportsController, FinancialIntelligenceController so
/// reporting queries don't compete with the write path.
///
/// The IDbConnectionSelector.UseReplica(true) call affects all direct DB connections made
/// via DbConnectionSelector.GetConnectionString() in this request scope.
/// The NpgsqlDataSource-backed AppDbContext is not re-routed at the EF level —
/// for full EF routing, register a separate AppDbContext with the replica data source
/// (see Program.cs ReadReplicaConnection configuration comment).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ReadReplicaFilter : Attribute, IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        var selector = context.HttpContext.RequestServices.GetService<IDbConnectionSelector>();
        selector?.UseReplica(true);
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        // Reset to primary after the action; selector is scoped so this is per-request.
        var selector = context.HttpContext.RequestServices.GetService<IDbConnectionSelector>();
        selector?.UseReplica(false);
    }
}
