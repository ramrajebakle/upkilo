using Microsoft.AspNetCore.Http;
using Upkilo.Core.Interfaces;

namespace Upkilo.Infrastructure.Services;

public class TenantProvider : ITenantProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? GetTenantId()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null) return null;

        if (context.Items.TryGetValue("TenantId", out var tenantIdObj) && tenantIdObj is string tenantIdStr)
        {
            if (Guid.TryParse(tenantIdStr, out var tenantId))
            {
                return tenantId;
            }
        }

        // Fallback to JWT claim if not in Items
        var tenantClaim = context.User?.FindFirst("tenant_id")?.Value;
        if (Guid.TryParse(tenantClaim, out var claimTenantId))
        {
            return claimTenantId;
        }

        return null;
    }

    public Guid? GetUserId()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null) return null;

        var userIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }

        return null;
    }

    public string? GetTimezone()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null) return null;

        if (context.Request.Headers.TryGetValue("X-Timezone", out var timezone))
        {
            return timezone.ToString();
        }

        return null;
    }
}
