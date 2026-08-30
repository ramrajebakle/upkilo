
using System.ComponentModel.DataAnnotations;
using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;
using Upkilo.Infrastructure.Services;
using Upkilo.API.Middleware;

namespace Upkilo.API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/domains")]
[Authorize(Roles = "Owner,Admin")]
[RequiresFeature(FeatureKeys.WhiteLabel)]
public class CustomDomainsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<CustomDomainsController> _logger;
    private readonly DomainManagementService _domainManagementService;
    private readonly IServiceScopeFactory _scopeFactory;

    // WL-03: exact-match set — substring .Contains() removed
    private static readonly HashSet<string> ValidCnameTargets = new(StringComparer.OrdinalIgnoreCase)
    {
        "proxy.upkilo.com",
        "app.upkilo.com"
    };

    // WL-08: names that must not be registered as custom domains
    private static readonly HashSet<string> BlockedHostnames = new(StringComparer.OrdinalIgnoreCase)
    {
        "upkilo.com", "localhost", "127.0.0.1", "::1", "0.0.0.0"
    };

    public CustomDomainsController(
        AppDbContext context,
        ITenantProvider tenantProvider,
        ILogger<CustomDomainsController> logger,
        DomainManagementService domainManagementService,
        IServiceScopeFactory scopeFactory)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _logger = logger;
        _domainManagementService = domainManagementService;
        _scopeFactory = scopeFactory;
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var domains = await _context.CustomDomains
            .Where(d => d.TenantId == tenantId)
            .Select(d => new
            {
                d.Id,
                d.Hostname,
                d.IsVerified,
                d.VerificationToken,
                d.SslStatus,
                d.LastVerifiedAt
            })
            .ToListAsync();

        return Ok(domains);
    }

    [HttpPost]
    public async Task<IActionResult> Add([FromBody] DomainDto request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        // WL-08: block internal/reserved hostnames and *.upkilo.com
        if (BlockedHostnames.Contains(request.Hostname) ||
            request.Hostname.EndsWith(".upkilo.com", StringComparison.OrdinalIgnoreCase) ||
            IPAddress.TryParse(request.Hostname, out _))
        {
            return BadRequest(new { error = "This hostname is not allowed." });
        }

        // Check uniqueness across all tenants
        if (await _context.CustomDomains.IgnoreQueryFilters().AnyAsync(d => d.Hostname == request.Hostname))
            return BadRequest(new { error = "Domain already registered." });

        var domain = new CustomDomain
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId.Value,
            Hostname = request.Hostname,
            VerificationToken = $"upkilo-verify-{Guid.NewGuid():N}",
            IsVerified = false,
            SslStatus = DomainSslStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.CustomDomains.Add(domain);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            domain.Id,
            domain.Hostname,
            domain.VerificationToken,
            Instructions = $"Add a TXT record to your DNS with value: {domain.VerificationToken}"
        });
    }

    [HttpPost("{id}/verify")]
    public async Task<IActionResult> VerifyDomain(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var domain = await _context.CustomDomains
            .FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tenantId);

        if (domain == null) return NotFound();

        bool verified = false;
        try
        {
            var lookup = new DnsClient.LookupClient();

            // Primary: TXT record ownership proof
            var txtResult = await lookup.QueryAsync(domain.Hostname, DnsClient.QueryType.TXT);
            foreach (var record in txtResult.Answers.TxtRecords())
            {
                if (record.Text.Contains(domain.VerificationToken))
                {
                    verified = true;
                    break;
                }
            }

            // Fallback: CNAME pointing to a known Upkilo target
            // WL-03: exact match against ValidCnameTargets — no substring match
            if (!verified)
            {
                var cnameResult = await lookup.QueryAsync(domain.Hostname, DnsClient.QueryType.CNAME);
                foreach (var record in cnameResult.Answers.CnameRecords())
                {
                    var canonical = record.CanonicalName.Value.TrimEnd('.');
                    if (ValidCnameTargets.Contains(canonical))
                    {
                        verified = true;
                        break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DNS verification error for {Hostname}", domain.Hostname);
            return BadRequest(new { error = "DNS verification error", message = $"An error occurred during DNS lookup: {ex.Message}" });
        }

        if (verified)
        {
            domain.IsVerified = true;
            domain.LastVerifiedAt = DateTime.UtcNow;
            // WL-07: SSL provisioning is async — stay Pending until cert is issued
            domain.SslStatus = DomainSslStatus.Pending;
            domain.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Domain {Hostname} verified for tenant {TenantId}", domain.Hostname, tenantId);

            // Fire SSL provisioning in background — Azure cert issuance takes seconds to minutes.
            var hostnameForBg = domain.Hostname;
            var domainIdForBg = domain.Id;
            _ = Task.Run(async () =>
            {
                await using var bgScope = _scopeFactory.CreateAsyncScope();
                var bgDb = bgScope.ServiceProvider.GetRequiredService<AppDbContext>();
                var bgSvc = bgScope.ServiceProvider.GetRequiredService<DomainManagementService>();
                var bgLog = bgScope.ServiceProvider.GetRequiredService<ILogger<CustomDomainsController>>();
                try
                {
                    await bgSvc.ProvisionCertificateAsync(hostnameForBg);
                    var d = await bgDb.CustomDomains.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == domainIdForBg);
                    if (d != null) { d.SslStatus = DomainSslStatus.Active; d.UpdatedAt = DateTime.UtcNow; await bgDb.SaveChangesAsync(); }
                }
                catch (Exception ex)
                {
                    bgLog.LogError(ex, "SSL provisioning failed for {Hostname}", hostnameForBg);
                    var d = await bgDb.CustomDomains.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == domainIdForBg);
                    if (d != null) { d.SslStatus = DomainSslStatus.Failed; d.UpdatedAt = DateTime.UtcNow; await bgDb.SaveChangesAsync(); }
                }
            });

            return Ok(new { domain.Id, domain.Hostname, domain.IsVerified, sslStatus = domain.SslStatus.ToString(), message = "Domain verified. SSL certificate provisioning started — please allow up to 15 minutes." });
        }

        return BadRequest(new { success = false, message = "Verification failed. DNS records not found yet." });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var domain = await _context.CustomDomains
            .FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tenantId);

        if (domain == null) return NotFound();

        _context.CustomDomains.Remove(domain);
        await _context.SaveChangesAsync();

        return Ok(new { success = true });
    }
}

// WL-08: validated hostname DTO
public class DomainDto
{
    [Required(ErrorMessage = "Hostname is required.")]
    [MaxLength(253)]
    [RegularExpression(
        @"^(?:[a-zA-Z0-9](?:[a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?\.)+[a-zA-Z]{2,}$",
        ErrorMessage = "Hostname must be a valid fully-qualified domain name (e.g. booking.example.com).")]
    public string Hostname { get; set; } = string.Empty;
}
