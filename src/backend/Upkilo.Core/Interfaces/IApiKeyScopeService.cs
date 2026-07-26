namespace Upkilo.Core.Interfaces;

public interface IApiKeyScopeService
{
    Task<bool> ValidateScopeAsync(string plainApiKey, string requiredScope);
}
