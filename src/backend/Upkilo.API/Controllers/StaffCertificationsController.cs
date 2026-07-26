using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.Infrastructure.Data;
using Upkilo.Core.Interfaces;
using Upkilo.Core.Entities;

namespace Upkilo.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize(Roles = "Admin,Owner,Staff")]
public class StaffCertificationsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public StaffCertificationsController(AppDbContext context, ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    [HttpGet]
    public async Task<IActionResult> GetCertifications([FromQuery] Guid? staffId)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var query = _context.StaffCertifications.Where(c => c.TenantId == tenantId);

        if (!User.IsInRole("Admin") && !User.IsInRole("Owner"))
        {
            // Staff seeing their own
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var staffMember = await _context.StaffMembers.FirstOrDefaultAsync(s => s.UserId.ToString() == userId);
            if (staffMember == null) return Forbid();
            query = query.Where(c => c.StaffId == staffMember.Id);
        }
        else if (staffId.HasValue)
        {
            query = query.Where(c => c.StaffId == staffId.Value);
        }

        var certs = await query.OrderBy(c => c.ExpirationDate).ToListAsync();
        return Ok(certs);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Owner")]
    public async Task<IActionResult> AddCertification([FromBody] CreateCertRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var cert = new StaffCertification
        {
            TenantId = tenantId.Value,
            StaffId = request.StaffId,
            Name = request.Name,
            IssuingAuthority = request.IssuingAuthority,
            IssueDate = request.IssueDate,
            ExpirationDate = request.ExpirationDate,
            DocumentUrl = request.DocumentUrl
        };

        _context.StaffCertifications.Add(cert);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCertifications), new { staffId = cert.StaffId }, cert);
    }
}

public class CreateCertRequest
{
    public Guid StaffId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? IssuingAuthority { get; set; }
    public DateTime IssueDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public string? DocumentUrl { get; set; }
}
