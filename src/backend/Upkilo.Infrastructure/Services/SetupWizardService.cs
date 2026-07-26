using Microsoft.Extensions.Logging;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

public class SetupWizardService : ISetupWizardService
{
    private readonly AppDbContext _context;
    private readonly ILogger<SetupWizardService> _logger;

    public SetupWizardService(AppDbContext context, ILogger<SetupWizardService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<SetupProgress> GetProgressAsync(Guid tenantId)
    {
        var progress = _context.Set<SetupProgress>()
            .FirstOrDefault(p => p.TenantId == tenantId);

        if (progress == null)
        {
            progress = new SetupProgress
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId
            };
            _context.Set<SetupProgress>().Add(progress);
            await _context.SaveChangesAsync();
        }

        return progress;
    }

    public async Task<SetupProgress> CompleteStepAsync(Guid tenantId, string stepName)
    {
        var progress = await GetProgressAsync(tenantId);

        switch (stepName.ToLower())
        {
            case "profile":
                progress.ProfileCompleted = true;
                break;
            case "services":
                progress.ServicesCompleted = true;
                break;
            case "staff":
                progress.StaffCompleted = true;
                break;
            case "availability":
                progress.AvailabilityCompleted = true;
                break;
            case "integrations":
                progress.IntegrationsCompleted = true;
                break;
            default:
                _logger.LogWarning("Unknown setup step: {StepName}", stepName);
                break;
        }

        progress.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Tenant {TenantId} completed setup step: {StepName}", tenantId, stepName);
        return progress;
    }

    public async Task<SetupProgress> ResetProgressAsync(Guid tenantId)
    {
        var progress = await GetProgressAsync(tenantId);
        
        progress.ProfileCompleted = false;
        progress.ServicesCompleted = false;
        progress.StaffCompleted = false;
        progress.AvailabilityCompleted = false;
        progress.IntegrationsCompleted = false;
        progress.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return progress;
    }
}
