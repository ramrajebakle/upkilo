// This file is intentionally excluded from non-Development builds via the project's
// conditional compilation guard in Program.cs (RegisterApiTestControllerInDevelopment).
// The controller is only reachable when ASPNETCORE_ENVIRONMENT=Development.
#if DEBUG || DEVELOPMENT
using Microsoft.AspNetCore.Mvc;
using Upkilo.Infrastructure.Services;
using Upkilo.AI.Interfaces;

namespace Upkilo.API.Controllers;

[ApiController]
[Route("api/test")]
[ApiExplorerSettings(IgnoreApi = true)]
public class ApiTestController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;

    public ApiTestController(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    [HttpGet("health")]
    public IActionResult Health()
        => Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow });

    [HttpGet("ai-check")]
    // IAIService is injected per-action; see ServicesController for why a constructor
    // dependency here made every endpoint on this controller construct the AI stack.
    public async Task<IActionResult> AiCheck([FromServices] IAIService aiService)
    {
        try
        {
            var result = await aiService.GeneralQueryAsync("ping");
            return Ok(new { Success = true, Response = result });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Success = false, Error = ex.Message });
        }
    }

    [HttpPost("audit-ping")]
    public IActionResult AuditPing()
    {
        _auditLogService.Log(new Upkilo.Core.Entities.AuditEntry
        {
            Action = "TestPing",
            Details = "API Test Controller Pinged",
            EntityType = "System",
            PerformedAt = DateTime.UtcNow
        });
        return Ok(new { Status = "Audit recorded (buffered)" });
    }
}
#endif
