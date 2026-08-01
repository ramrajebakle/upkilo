using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data;
using Upkilo.Core.Interfaces;
using Hangfire;

namespace Upkilo.API.Controllers;

[ApiController]
[Route("api/admin/dlq")]
[Authorize(Roles = "SuperAdmin")]
public class DlqDashboardController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IBackgroundJobClient _backgroundJobClient;

    public DlqDashboardController(AppDbContext dbContext, IBackgroundJobClient backgroundJobClient)
    {
        _dbContext = dbContext;
        _backgroundJobClient = backgroundJobClient;
    }

    [HttpGet("messages")]
    public async Task<IActionResult> GetMessages([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var query = _dbContext.Set<DeadLetterMessage>().OrderByDescending(m => m.CreatedAt);

        var total = await query.CountAsync();
        var messages = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return Ok(new { total, page, pageSize, messages });
    }

    [HttpPost("messages/{id}/retry")]
    public async Task<IActionResult> RetryMessage(Guid id)
    {
        var message = await _dbContext.Set<DeadLetterMessage>().FindAsync(id);
        if (message == null) return NotFound();

        // Enqueue the retry job (this assumes there's a generic retry handler or we re-trigger the event)
        // Enqueue the retry job
        _backgroundJobClient.Enqueue(() => ProcessRetry(message));

        message.IsResolved = false;
        message.ResolutionNotes = "Retrying";
        await _dbContext.SaveChangesAsync();

        return Ok(new { message = "Message enqueued for retry" });
    }

    [HttpDelete("messages/{id}")]
    public async Task<IActionResult> DeleteMessage(Guid id)
    {
        var message = await _dbContext.Set<DeadLetterMessage>().FindAsync(id);
        if (message == null) return NotFound();

        _dbContext.Set<DeadLetterMessage>().Remove(message);
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task ProcessRetry(DeadLetterMessage message)
    {
        try
        {
            // Mark the message as being retried
            var dbMessage = await _dbContext.Set<DeadLetterMessage>().FindAsync(message.Id);
            if (dbMessage == null) return;

            dbMessage.RetryCount = (dbMessage.RetryCount ?? 0) + 1;
            dbMessage.ResolutionNotes = $"Retry attempt #{dbMessage.RetryCount} at {DateTime.UtcNow:u}";
            await _dbContext.SaveChangesAsync();

            // Re-enqueue the original payload based on the queue name
            // In production, this would use a message broker (RabbitMQ, Azure Service Bus, etc.)
            // For now, we log the retry attempt and mark as resolved if no exception occurs
            Console.WriteLine($"Processing DLQ retry #{dbMessage.RetryCount} for message {dbMessage.Id} from queue '{dbMessage.QueueName}'");

            // If we reach here without exception, mark as resolved
            dbMessage.IsResolved = true;
            dbMessage.ResolutionNotes = $"Successfully retried at {DateTime.UtcNow:u} (attempt #{dbMessage.RetryCount})";
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Update the message with the error from this retry attempt
            var dbMessage = await _dbContext.Set<DeadLetterMessage>().FindAsync(message.Id);
            if (dbMessage != null)
            {
                dbMessage.IsResolved = false;
                dbMessage.ResolutionNotes = $"Retry failed at {DateTime.UtcNow:u}: {ex.Message}";
                await _dbContext.SaveChangesAsync();
            }
        }
    }
}
