using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Upkilo.API.Attributes;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
[FeatureGuard("sms_reminders")]
public class CommunicationsController : ControllerBase
{
    private readonly ISmsService _smsService;
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public CommunicationsController(
        ISmsService smsService,
        AppDbContext context,
        ITenantProvider tenantProvider)
    {
        _smsService = smsService;
        _context = context;
        _tenantProvider = tenantProvider;
    }

    [HttpPost("sms")]
    public async Task<IActionResult> SendSms([FromBody] SendClientSmsRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var client = await _context.Clients
            .FirstOrDefaultAsync(c => c.Id == request.ClientId && c.TenantId == tenantId);

        if (client == null) return NotFound("Client not found");

        if (string.IsNullOrEmpty(client.Phone)) return BadRequest("Client has no phone number");

        var result = await _smsService.SendSmsAsync(tenantId.Value, client.Phone, request.Message, client.Id);

        // Log to database
        var log = new CommunicationLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId.Value,
            ClientId = client.Id,
            Type = CommunicationType.SMS,
            Direction = CommunicationDirection.Outbound,
            Body = request.Message,
            Status = result.Success ? CommunicationStatus.Sent : CommunicationStatus.Failed,
            ReferenceId = result.MessageId,
            CreatedAt = DateTime.UtcNow
        };

        if (!result.Success)
        {
            log.Metadata["Error"] = result.Error ?? "Unknown error";
        }

        _context.CommunicationLogs.Add(log);
        try 
        {
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
           // Log error but don't fail request if SMS sent
        }

        if (!result.Success) return BadRequest($"Failed to send SMS: {result.Error}");

        return Ok(new { success = true });
    }
}

public class SendClientSmsRequest
{
    public Guid ClientId { get; set; }
    public string Message { get; set; }
}

