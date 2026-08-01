using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Upkilo.Core.Interfaces;

namespace Upkilo.API.Middleware;

/// <summary>
/// Middleware to intercept requests with X-Sandbox-Mode and route them to a virtual environment.
/// </summary>
public class SandboxMiddleware
{
    private readonly RequestDelegate _next;

    public SandboxMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 1. Detect Sandbox Headers
        if (context.Request.Headers.TryGetValue("X-Sandbox-Mode", out var sandboxMode) &&
            sandboxMode.ToString().ToLower() == "true")
        {
            // 2. Identify the Sandbox ID (if provided)
            context.Request.Headers.TryGetValue("X-Sandbox-Id", out var sandboxId);

            // 3. Update the request context items for downstream services
            context.Items["IsSandboxRequest"] = true;
            context.Items["SandboxId"] = sandboxId.ToString();

            // 4. Inject Sandbox-specific Tenant ID if we use the "Shadow Tenant" approach
            // In a real implementation, this would lookup the SandboxId to find its virtual TenantId.
            if (!string.IsNullOrEmpty(sandboxId))
            {
                // Logic to swap TenantID for the sandbox isolation
                // context.Items["TenantId"] = "sandbox_" + sandboxId;
            }
        }

        await _next(context);
    }
}
