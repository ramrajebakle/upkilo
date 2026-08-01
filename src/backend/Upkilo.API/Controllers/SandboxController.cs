using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Upkilo.Core.Interfaces;

namespace Upkilo.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize] // Requires a developer account or organization admin
public class SandboxController : ControllerBase
{
    private readonly ISandboxService _sandboxService;

    public SandboxController(ISandboxService sandboxService)
    {
        _sandboxService = sandboxService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] string? seedConfig)
    {
        // Get user ID from JWT
        if (!Guid.TryParse((User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value), out var userId))
            return Unauthorized(new { error = "Invalid user identity in token." });

        var sandbox = await _sandboxService.CreateSandboxAsync(userId, seedConfig);

        return CreatedAtAction(nameof(Get), new { id = sandbox.SandboxId }, sandbox);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var isValid = await _sandboxService.IsSandboxValidAsync(id);
        if (!isValid)
            return NotFound(new { error = "Sandbox not found or expired." });

        await _sandboxService.RecordAccessAsync(id);
        return Ok(new { SandboxId = id, Status = "Active" });
    }

    [HttpPost("{id}/reset")]
    public async Task<IActionResult> Reset(string id)
    {
        await _sandboxService.ResetSandboxAsync(id);
        return Ok(new { Message = "Sandbox reset successfully" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        await _sandboxService.DeleteSandboxAsync(id);
        return NoContent();
    }
}
