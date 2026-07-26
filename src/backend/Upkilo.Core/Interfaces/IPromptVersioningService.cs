using Upkilo.Core.Entities;

namespace Upkilo.Core.Interfaces;

public interface IPromptVersioningService
{
    Task<PromptVersion?> GetActivePromptAsync(string promptKey, Guid tenantId);
    Task<List<PromptVersion>> GetVersionHistoryAsync(string promptKey, Guid tenantId);
    Task<PromptVersion> CreateVersionAsync(PromptVersion newVersion);
    Task<PromptVersion?> RollbackToVersionAsync(Guid versionId, Guid tenantId);
    Task<List<string>> GetPromptKeysAsync(Guid tenantId);
}
