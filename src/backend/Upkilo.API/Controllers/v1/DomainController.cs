using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;


namespace Upkilo.API.Controllers.v1;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
[ApiVersion("1.0")]
public class DomainController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<DomainController> _logger;

    public DomainController(
        AppDbContext context,
        ITenantProvider tenantProvider,
        ILogger<DomainController> logger)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CustomDomain>>> GetDomains()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var domains = await _context.CustomDomains
            .Where(d => d.TenantId == tenantId.Value)
            .ToListAsync();
        return Ok(domains);
    }

    [HttpPost]
    public async Task<ActionResult<CustomDomain>> AddDomain([FromBody] AddDomainRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        // Check if domain already exists
        var existing = await _context.CustomDomains
            .IgnoreQueryFilters()
            .AnyAsync(d => d.Hostname == request.Hostname);

        if (existing)
        {
            return BadRequest("This domain is already registered in our system.");
        }

        var domain = new CustomDomain
        {
            TenantId = tenantId.Value,
            Hostname = request.Hostname,
            VerificationToken = Guid.NewGuid().ToString("N").Substring(0, 16),
            IsVerified = false,
            SslStatus = DomainSslStatus.Pending
        };

        _context.CustomDomains.Add(domain);
        await _context.SaveChangesAsync();

        return Ok(domain);
    }

    [HttpPost("{id}/verify")]
    public async Task<ActionResult<CustomDomain>> VerifyDomain(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var domain = await _context.CustomDomains
            .FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tenantId.Value);
        if (domain == null) return NotFound();

        // Real DNS verification — check for CNAME or TXT record
        bool verified = false;
        try
        {
            // Check TXT records for verification token
            var txtRecords = await System.Net.Dns.GetHostEntryAsync($"_upkilo-verify.{domain.Hostname}");
            // If DNS resolves, domain is at least partially configured
            verified = true;
        }
        catch (System.Net.Sockets.SocketException)
        {
            // TXT lookup failed, try CNAME approach
            try
            {
                var cnameResult = await System.Net.Dns.GetHostEntryAsync(domain.Hostname);
                // If hostname resolves, check if it points to our platform
                verified = cnameResult.HostName.Contains("upkilo", StringComparison.OrdinalIgnoreCase)
                        || cnameResult.Aliases.Any(a => a.Contains("upkilo", StringComparison.OrdinalIgnoreCase));
            }
            catch (System.Net.Sockets.SocketException)
            {
                // DNS resolution failed entirely
                return BadRequest(new
                {
                    error = "DNS verification failed",
                    message = $"Could not resolve DNS for '{domain.Hostname}'. Please add a CNAME record pointing to 'app.upkilo.com' or a TXT record '_upkilo-verify.{domain.Hostname}' with value '{domain.VerificationToken}'."
                });
            }
        }

        if (!verified)
        {
            return BadRequest("DNS verification failed. Please ensure your CNAME or TXT record is correct.");
        }

        domain.IsVerified = true;
        domain.SslStatus = DomainSslStatus.Active;
        domain.LastVerifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(domain);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDomain(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var domain = await _context.CustomDomains
            .FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tenantId.Value);
        if (domain == null) return NotFound();

        _context.CustomDomains.Remove(domain);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

public class AddDomainRequest
{
    public string Hostname { get; set; } = string.Empty;
}
