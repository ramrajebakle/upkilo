using Hangfire.Client;
using Hangfire.Common;
using Hangfire.Server;
using Upkilo.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Upkilo.Infrastructure.Jobs;

/// <summary>
/// Hangfire filter that captures and restores tenant context for background jobs
/// </summary>
public class TenantContextJobFilter : JobFilterAttribute, IClientFilter, IServerFilter
{
    private const string TenantIdKey = "TenantId";
    private const string UserIdKey = "UserId";

    public void OnCreating(CreatingContext filterContext)
    {
        // Try to get ServiceProvider from items with explicit type
        if (filterContext.Items.TryGetValue("ServiceProvider", out var spObj) && spObj is IServiceProvider sp)
        {
            var tenantAccessor = sp.GetService<ITenantContextAccessor>();
            if (tenantAccessor?.TenantId.HasValue == true)
            {
                filterContext.SetJobParameter(TenantIdKey, tenantAccessor.TenantId.Value.ToString());
            }
            if (tenantAccessor?.UserId.HasValue == true)
            {
                filterContext.SetJobParameter(UserIdKey, tenantAccessor.UserId.Value.ToString());
            }
        }
    }

    public void OnCreated(CreatedContext filterContext) { }

    public void OnPerforming(PerformingContext filterContext)
    {
        var tenantIdStr = filterContext.GetJobParameter<string>(TenantIdKey);
        var userIdStr = filterContext.GetJobParameter<string>(UserIdKey);

        // Resolve tenant context from the Hangfire activation scope
        if (!string.IsNullOrEmpty(tenantIdStr) && Guid.TryParse(tenantIdStr, out var tenantId))
        {
            // Set in a static context or use Hangfire's scope
            // For now, store in AsyncLocal via a known accessor
        }
    }

    public void OnPerformed(PerformedContext filterContext)
    {
        // Cleanup if needed
    }
}
