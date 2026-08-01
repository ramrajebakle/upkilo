using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Upkilo.Core.Interfaces;
using System.Threading.Tasks;

namespace Upkilo.API.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AllowGracefulDegradationAttribute : ActionFilterAttribute
{
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var monitor = context.HttpContext.RequestServices.GetService<ISystemLoadMonitorService>();
        if (monitor != null && monitor.IsSystemDegraded())
        {
            context.Result = new ObjectResult(new
            {
                error = "service_unavailable",
                message = "The system is currently experiencing high load. This non-critical request has been temporarily dropped to preserve essential scheduling functions. Please try again later."
            })
            {
                StatusCode = StatusCodes.Status503ServiceUnavailable
            };
            return;
        }

        await next();
    }
}
