using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Upkilo.API.Middleware;
using Upkilo.Core.Interfaces;
using Upkilo.Core.Queries;

namespace Upkilo.API.Controllers;

/// <summary>
/// CQRS read-model endpoints — fast, denormalized projections for calendar and dashboard.
/// All endpoints are read-only and never mutate state.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/read")]
[Authorize]
public class ReadModelsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ITenantProvider _tenantProvider;

    public ReadModelsController(IMediator mediator, ITenantProvider tenantProvider)
    {
        _mediator = mediator;
        _tenantProvider = tenantProvider;
    }

    private Guid GetTenantId() => _tenantProvider.GetTenantId()
        ?? throw new UnauthorizedAccessException("Tenant context not available");

    /// <summary>
    /// GET /api/v1/read/calendar?from=2026-01-01&to=2026-01-31
    /// Returns a flat, render-ready booking calendar read model.
    /// </summary>
    [HttpGet("calendar")]
    public async Task<IActionResult> GetCalendar(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] string? staffId = null,
        [FromQuery] string? serviceId = null,
        [FromQuery] string? status = null)
    {
        if (from >= to)
            return BadRequest(ApiResponse.Fail("'from' must be before 'to'"));

        var result = await _mediator.Send(new BookingCalendarQuery
        {
            TenantId = GetTenantId(),
            From = from,
            To = to,
            StaffId = staffId,
            ServiceId = serviceId,
            Status = status,
        });

        return Ok(ApiResponse<object>.Ok(result));
    }

    /// <summary>
    /// GET /api/v1/read/dashboard?period=30d
    /// Returns pre-aggregated KPIs — faster than hitting OLTP tables directly.
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboardAggregates([FromQuery] string period = "30d")
    {
        var validPeriods = new[] { "7d", "30d", "90d", "ytd" };
        if (!validPeriods.Contains(period))
            return BadRequest(ApiResponse.Fail($"Invalid period. Must be one of: {string.Join(", ", validPeriods)}"));

        var result = await _mediator.Send(new DashboardAggregateQuery
        {
            TenantId = GetTenantId(),
            Period = period,
        });

        return Ok(ApiResponse<object>.Ok(result));
    }
}
