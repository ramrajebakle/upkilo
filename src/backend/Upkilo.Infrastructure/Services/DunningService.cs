using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace Upkilo.Infrastructure.Services;

public class DunningService
{
    private readonly AppDbContext _context;
    private readonly ILogger<DunningService> _logger;

    public DunningService(AppDbContext context, ILogger<DunningService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task ProcessDunningCyclesAsync()
    {
        _logger.LogInformation("DunningService: Starting dunning cycle processing.");

        var activeCycles = await _context.DunningCycles
            .Where(d => d.Status == "Active" && d.NextAttemptAt <= DateTime.UtcNow)
            .ToListAsync();

        foreach (var cycle in activeCycles)
        {
            try
            {
                _logger.LogInformation("DunningService: Processing cycle {Id} for tenant {TenantId}.", cycle.Id, cycle.TenantId);
                
                cycle.AttemptCount++;
                cycle.LastAttemptAt = DateTime.UtcNow;

                if (cycle.AttemptCount >= 3)
                {
                    cycle.Status = "Failed";
                    _logger.LogWarning("DunningService: Cycle {Id} failed after 3 attempts. Suspending tenant.", cycle.Id);
                    // Trigger tenant suspension logic here if needed
                }
                else
                {
                    cycle.NextAttemptAt = DateTime.UtcNow.AddDays(3);
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DunningService: Failed to process cycle {Id}.", cycle.Id);
            }
        }
    }
}
