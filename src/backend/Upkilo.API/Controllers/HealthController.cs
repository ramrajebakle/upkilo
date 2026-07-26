using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Upkilo.API.Controllers;

/// <summary>
/// Health check controller — delegates to ASP.NET Core HealthCheckService
/// for real DB, Redis, and application status checks.
/// 
/// Primary endpoint: /health (mapped via MapHealthChecks in Program.cs)
/// This controller provides the versioned API alternative at /api/v1/health
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;
    private readonly HealthCheckService _healthCheckService;

    public HealthController(ILogger<HealthController> logger, HealthCheckService healthCheckService)
    {
        _logger = logger;
        _healthCheckService = healthCheckService;
    }

    /// <summary>
    /// Basic liveness check — lightweight, no dependency checks
    /// </summary>
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0",
            Environment = System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"
        });
    }

    /// <summary>
    /// Detailed readiness check — runs all registered health checks
    /// (PostgreSQL, Redis, Application memory/uptime)
    /// </summary>
    [HttpGet("ready")]
    public async Task<IActionResult> Ready()
    {
        var report = await _healthCheckService.CheckHealthAsync();

        var result = new
        {
            Status = report.Status.ToString(),
            Timestamp = DateTime.UtcNow,
            Duration = report.TotalDuration.TotalMilliseconds + "ms",
            Checks = report.Entries.ToDictionary(
                e => e.Key,
                e => new
                {
                    Status = e.Value.Status.ToString(),
                    Description = e.Value.Description,
                    Duration = e.Value.Duration.TotalMilliseconds + "ms",
                    Data = e.Value.Data
                })
        };

        if (report.Status == HealthStatus.Unhealthy)
        {
            _logger.LogWarning("Readiness check UNHEALTHY: {Details}", 
                string.Join(", ", report.Entries
                    .Where(e => e.Value.Status != HealthStatus.Healthy)
                    .Select(e => $"{e.Key}={e.Value.Status}")));
            return StatusCode(503, result);
        }

        return Ok(result);
    }
}
