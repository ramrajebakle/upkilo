using Upkilo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Upkilo.API.Jobs;

public class AIUsageCleanupJob
{
    private readonly AppDbContext _context;
    private readonly ILogger<AIUsageCleanupJob> _logger;

    public AIUsageCleanupJob(AppDbContext context, ILogger<AIUsageCleanupJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        _logger.LogInformation("Starting AI Usage Log cleanup...");

        // Keep last 90 days of detailed usage logs
        var cutoffDate = DateTime.UtcNow.AddDays(-90);

        var logsToRemove = await _context.AIUsageLogs
            .Where(l => l.CreatedAt < cutoffDate)
            .ToListAsync();

        if (logsToRemove.Any())
        {
            _context.AIUsageLogs.RemoveRange(logsToRemove);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Cleaned up {Count} old AI usage logs.", logsToRemove.Count);
        }
        else
        {
            _logger.LogInformation("No old AI usage logs found to clean up.");
        }
    }
}
