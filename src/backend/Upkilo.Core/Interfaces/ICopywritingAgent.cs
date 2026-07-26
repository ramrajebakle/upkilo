namespace Upkilo.Core.Interfaces;

public interface ICopywritingAgent
{
    Task<string> GenerateEmailContentAsync(Guid tenantId, string businessName, string serviceName, string targetAudience, string goal);
    Task<string> GenerateSmsContentAsync(Guid tenantId, string businessName, string serviceName, string goal);
    Task<string> GenerateSocialMediaPostAsync(Guid tenantId, string platform, string businessName, string serviceName, string topic);
}
