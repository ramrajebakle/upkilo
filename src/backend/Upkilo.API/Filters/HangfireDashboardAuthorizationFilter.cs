using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;

namespace Upkilo.API.Filters;

/// <summary>
/// SECURE: Authorization filter for Hangfire dashboard.
/// Only allows SuperAdmins or specifically authorized IPs in production.
/// </summary>
public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        // In development, allow all
        var env = httpContext.RequestServices.GetService(typeof(IWebHostEnvironment)) as IWebHostEnvironment;
        if (env?.IsDevelopment() == true) return true;

        // In production, check for SuperAdmin role
        return httpContext.User.Identity?.IsAuthenticated == true && 
               httpContext.User.IsInRole("SuperAdmin");
    }
}
