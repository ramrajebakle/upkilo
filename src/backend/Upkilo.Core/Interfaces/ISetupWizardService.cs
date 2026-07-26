using Upkilo.Core.Entities;

namespace Upkilo.Core.Interfaces;

public interface ISetupWizardService
{
    Task<SetupProgress> GetProgressAsync(Guid tenantId);
    Task<SetupProgress> CompleteStepAsync(Guid tenantId, string stepName);
    Task<SetupProgress> ResetProgressAsync(Guid tenantId);
}
