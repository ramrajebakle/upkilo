using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Upkilo.Core.Interfaces;
using Upkilo.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Upkilo.Infrastructure.Data;
using Upkilo.Infrastructure.Services;

namespace Upkilo.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class CalendarController : ControllerBase
{
    private readonly IEnumerable<ICalendarService> _calendarServices;
    private readonly AppDbContext _context;
    private readonly ILogger<CalendarController> _logger;

    public CalendarController(IEnumerable<ICalendarService> calendarServices, AppDbContext context, ILogger<CalendarController> logger)
    {
        _calendarServices = calendarServices;
        _context = context;
        _logger = logger;
    }

    [HttpGet("auth-url")]
    public IActionResult GetAuthUrl([FromQuery] string provider, [FromQuery] Guid staffId)
    {
        var service = GetService(provider);
        if (service == null) return BadRequest("Unsupported provider");

        var url = service.GetAuthUrl(provider, staffId);
        return Ok(new { url });
    }

    [HttpPost("connect")]
    public async Task<IActionResult> Connect([FromBody] ConnectRequest request)
    {
        var service = GetService(request.Provider);
        if (service == null) return BadRequest("Unsupported provider");

        try
        {
            var token = await service.ConnectAsync(request.Provider, request.StaffId, request.Code);
            return Ok(new { message = "Connected successfully", provider = token.Provider });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error connecting to {Provider}", request.Provider);
            return StatusCode(500, "Authentication failed");
        }
    }

    [HttpPost("sync/{staffId}")]
    public async Task<IActionResult> Sync(Guid staffId)
    {
        var tokens = await _context.CalendarSyncTokens
            .Where(t => t.StaffId == staffId)
            .ToListAsync();

        if (!tokens.Any()) return NotFound("No active calendar connections found");

        foreach (var token in tokens)
        {
            var service = GetService(token.Provider);
            if (service != null)
            {
                await service.SyncBookingsAsync(staffId);
            }
        }

        return Ok(new { message = "Sync initiated" });
    }

    [HttpGet("connections/{staffId}")]
    public async Task<IActionResult> GetConnections(Guid staffId)
    {
        var connections = await _context.CalendarSyncTokens
            .Where(t => t.StaffId == staffId)
            .Select(t => new
            {
                t.Provider,
                t.LastSyncAt,
                t.ExpiresAt
            })
            .ToListAsync();

        return Ok(connections);
    }

    private ICalendarService? GetService(string provider)
    {
        return provider.ToLower() switch
        {
            "google" => _calendarServices.OfType<GoogleCalendarService>().FirstOrDefault(),
            "outlook" => _calendarServices.OfType<OutlookCalendarService>().FirstOrDefault(),
            _ => null
        };
    }
}

public record ConnectRequest(string Provider, Guid StaffId, string Code);

