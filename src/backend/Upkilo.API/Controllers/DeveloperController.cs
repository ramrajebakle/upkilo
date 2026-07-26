using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Upkilo.API.Attributes;
using Upkilo.Core.Interfaces;
using Upkilo.Core.Entities;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize(Roles = "Developer,Admin,EnterpriseAdmin")]
    [FeatureGuard("api_access")]
    public class DeveloperController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ITenantProvider _tenantProvider;

        public DeveloperController(AppDbContext context, ITenantProvider tenantProvider)
        {
            _context = context;
            _tenantProvider = tenantProvider;
        }

        private Guid GetTenantId() => _tenantProvider.GetTenantId() 
            ?? throw new UnauthorizedAccessException("Tenant context not available");

        [HttpPost("apps")]
        public async Task<IActionResult> RegisterApp([FromBody] CreateAppRequest request)
        {
            var tenantId = GetTenantId();
            var clientId = "up_" + Guid.NewGuid().ToString("N");
            var clientSecret = Guid.NewGuid().ToString("N");

            var app = new OAuthApp
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                AppName = request.Name,
                Description = request.Description,
                ClientId = clientId,
                ClientSecretHash = HashSecret(clientSecret),
                RedirectUris = string.Join(",", request.RedirectUris),
                Scopes = string.Join(",", request.Scopes),
                CreatedAt = DateTime.UtcNow
            };

            _context.OAuthApps.Add(app);
            await _context.SaveChangesAsync();

            return Ok(new 
            { 
                clientId, 
                clientSecret, // ONLY returned once
                message = "App registered. Store your secret safely; it cannot be retrieved again." 
            });
        }

        [HttpGet("apps")]
        public async Task<IActionResult> GetApps()
        {
            var apps = await _context.OAuthApps
                .Where(a => a.TenantId == GetTenantId())
                .Select(a => new { a.Id, a.AppName, a.ClientId, a.CreatedAt, a.IsActive })
                .ToListAsync();

            return Ok(apps);
        }

        [HttpDelete("apps/{id}")]
        public async Task<IActionResult> DeleteApp(Guid id)
        {
            var app = await _context.OAuthApps.FirstOrDefaultAsync(a => a.Id == id && a.TenantId == GetTenantId());
            if (app == null) return NotFound();

            _context.OAuthApps.Remove(app);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Day 59: GET /api/v1/developer/rate-limits — Returns rate limit quotas for the tenant's plan.
        /// VULN-A13 FIX: removed [AllowAnonymous] — authenticated developers can query their own tier.
        /// </summary>
        [HttpGet("rate-limits")]
        public async Task<IActionResult> GetRateLimits()
        {
            Guid? tenantId;
            try { tenantId = GetTenantId(); } catch { tenantId = null; }

            // Default limits by plan tier
            var tier = "Starter";
            if (tenantId.HasValue)
            {
                var tenant = await _context.Tenants.FindAsync(tenantId.Value);
                tier = tenant?.SubscriptionTier.ToString() ?? "Starter";
            }

            var limits = tier switch
            {
                "Business" => new RateLimitInfo(10000, 500, 100),
                "Pro" => new RateLimitInfo(5000, 200, 50),
                _ => new RateLimitInfo(1000, 60, 10)
            };

            return Ok(new
            {
                tier,
                requestsPerDay = limits.PerDay,
                requestsPerMinute = limits.PerMinute,
                webhooksPerHour = limits.WebhooksPerHour,
                documentation = "https://docs.upkilo.com/api/rate-limits",
                headers = new
                {
                    remaining = "X-RateLimit-Remaining",
                    limit = "X-RateLimit-Limit",
                    reset = "X-RateLimit-Reset"
                }
            });
        }

        /// <summary>
        /// Day 59: GET /api/v1/developer/portal — Developer portal overview (webhooks, scopes, sandbox status).
        /// </summary>
        [HttpGet("portal")]
        public async Task<IActionResult> GetPortal()
        {
            var tenantId = GetTenantId();

            var appCount = await _context.OAuthApps.CountAsync(a => a.TenantId == tenantId && a.IsActive);
            var webhookCount = await _context.Webhooks.CountAsync(w => w.TenantId == tenantId && w.IsActive);

            return Ok(new
            {
                apps = appCount,
                webhooks = webhookCount,
                availableScopes = new[]
                {
                    "bookings:read", "bookings:write",
                    "clients:read", "clients:write",
                    "services:read",
                    "payments:read",
                    "analytics:read"
                },
                sandboxEnabled = true,
                sandboxBaseUrl = "https://sandbox.upkilo.com/api/v1",
                docsUrl = "https://docs.upkilo.com",
                openApiUrl = "/swagger/v1/swagger.json"
            });
        }

        private string HashSecret(string secret)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(secret);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }
    }

    internal record RateLimitInfo(int PerDay, int PerMinute, int WebhooksPerHour);

    public class CreateAppRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string[] RedirectUris { get; set; } = Array.Empty<string>();
        public string[] Scopes { get; set; } = Array.Empty<string>();
    }
}
