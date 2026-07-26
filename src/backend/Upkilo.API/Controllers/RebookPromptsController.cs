using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Controllers;

/// <summary>
/// Automated rebooking prompt rules. Backs the /bookings/rebook page.
/// </summary>
[ApiController]
[Route("api/v1/automation/rebook-prompts")]
[Authorize]
public class RebookPromptsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public RebookPromptsController(AppDbContext context, ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    public record RebookPromptRequest(
        string name, string trigger, int? triggerValue, Guid? serviceId, string? serviceName,
        string channel, string message, string? subject);

    private static object Project(RebookPrompt p) => new
    {
        id = p.Id,
        name = p.Name,
        trigger = p.Trigger,
        triggerValue = p.TriggerValue,
        serviceId = p.ServiceId,
        serviceName = p.ServiceName,
        channel = p.Channel,
        message = p.Message,
        subject = p.Subject,
        isActive = p.IsActive,
        sendCount = p.SendCount,
        conversionCount = p.ConversionCount,
        conversionRate = p.ConversionRate,
        lastSent = p.LastSent,
        createdAt = p.CreatedAt,
    };

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var prompts = await _context.RebookPrompts
            .Where(p => p.TenantId == tenantId.Value && !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
        return Ok(prompts.Select(Project));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] RebookPromptRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();
        if (string.IsNullOrWhiteSpace(request.name)) return BadRequest(new { message = "Name is required." });
        if (string.IsNullOrWhiteSpace(request.message)) return BadRequest(new { message = "Message is required." });

        var prompt = new RebookPrompt
        {
            TenantId = tenantId.Value,
            Name = request.name.Trim(),
            Trigger = string.IsNullOrWhiteSpace(request.trigger) ? "days_since_last_visit" : request.trigger,
            TriggerValue = request.triggerValue,
            ServiceId = request.serviceId,
            ServiceName = request.serviceName,
            Channel = string.IsNullOrWhiteSpace(request.channel) ? "sms" : request.channel,
            Message = request.message,
            Subject = request.subject,
            IsActive = false,
        };
        _context.RebookPrompts.Add(prompt);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(List), new { id = prompt.Id }, Project(prompt));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] RebookPromptRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var prompt = await _context.RebookPrompts
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId.Value && !p.IsDeleted);
        if (prompt == null) return NotFound();

        prompt.Name = request.name?.Trim() ?? prompt.Name;
        prompt.Trigger = request.trigger ?? prompt.Trigger;
        prompt.TriggerValue = request.triggerValue;
        prompt.ServiceId = request.serviceId;
        prompt.ServiceName = request.serviceName;
        prompt.Channel = request.channel ?? prompt.Channel;
        prompt.Message = request.message ?? prompt.Message;
        prompt.Subject = request.subject;
        prompt.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(Project(prompt));
    }

    [HttpPost("{id:guid}/toggle")]
    public async Task<IActionResult> Toggle(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var prompt = await _context.RebookPrompts
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId.Value && !p.IsDeleted);
        if (prompt == null) return NotFound();

        prompt.IsActive = !prompt.IsActive;
        prompt.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(Project(prompt));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var prompt = await _context.RebookPrompts
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId.Value && !p.IsDeleted);
        if (prompt == null) return NotFound();

        prompt.IsDeleted = true;
        prompt.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
