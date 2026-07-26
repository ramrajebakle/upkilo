using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Upkilo.Infrastructure.Data;
using Upkilo.Infrastructure.Security;
using Upkilo.Infrastructure.Services;

namespace Upkilo.Infrastructure.Controllers;

/// <summary>
/// Implements Task 1425: 10 operational dashboards (SuperAdmin entry point)
/// Implements Task 1335: Penetration testing (Reporting UI)
/// </summary>
[Authorize(Roles = "SuperAdmin")]
[ApiController]
[Route("api/superadmin/security")]
public class SecurityAuditController : ControllerBase
{
    private readonly SecurityScannerService _scanner;
    private readonly OperationalDashboardService _dashboard;
    private readonly AppDbContext _context;

    public SecurityAuditController(SecurityScannerService scanner, OperationalDashboardService dashboard, AppDbContext context)
    {
        _scanner = scanner;
        _dashboard = dashboard;
        _context = context;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetSecurityDashboard()
    {
        var metrics = await _dashboard.GetSystemHealthDashboardsAsync();
        return Ok(metrics);
    }

    [HttpPost("scan/{tenantId}")]
    public async Task<IActionResult> TriggerManualScan(Guid tenantId)
    {
        var result = await _scanner.RunAutoScanAsync(tenantId);
        return Ok(result);
    }

    [HttpGet("audit-logs")]
    public async Task<IActionResult> GetGlobalAuditLogs(
        [FromQuery] int skip = 0,
        [FromQuery] int count = 100,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] string? entityType = null,
        [FromQuery] string? action = null)
    {
        count = Math.Clamp(count, 1, 500);

        var query = _context.AuditEntries
            .IgnoreQueryFilters() // SuperAdmin: bypass tenant filter
            .AsNoTracking()
            .AsQueryable();

        if (from.HasValue) query = query.Where(a => a.Timestamp >= from.Value);
        if (to.HasValue) query = query.Where(a => a.Timestamp <= to.Value);
        if (!string.IsNullOrWhiteSpace(entityType)) query = query.Where(a => a.EntityType == entityType);
        if (!string.IsNullOrWhiteSpace(action)) query = query.Where(a => a.Action == action);

        var total = await query.CountAsync();
        var entries = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip(skip)
            .Take(count)
            .Select(a => new
            {
                a.Id,
                a.EntityType,
                a.EntityId,
                a.Action,
                a.UserId,
                a.UserName,
                a.IpAddress,
                a.UserAgent,
                a.Timestamp,
                a.TenantId,
                ChangedFields = a.ChangedFields
            })
            .ToListAsync();

        return Ok(new { total, skip, count, entries });
    }
}
