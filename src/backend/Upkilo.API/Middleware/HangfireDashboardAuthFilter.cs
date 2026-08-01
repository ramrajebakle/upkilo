using Hangfire.Dashboard;

namespace Upkilo.API.Middleware;

/// <summary>
/// Restricts Hangfire Dashboard access to authenticated SuperAdmin users only.
/// Prevents unauthorized job viewing/triggering in production.
/// </summary>
public class HangfireDashboardAuthFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        // In development, allow all access for debugging
        var env = httpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
        if (env.IsDevelopment())
            return true;

        // In production, require authenticated SuperAdmin
        return httpContext.User.Identity?.IsAuthenticated == true &&
               httpContext.User.IsInRole("SuperAdmin");
    }
}
