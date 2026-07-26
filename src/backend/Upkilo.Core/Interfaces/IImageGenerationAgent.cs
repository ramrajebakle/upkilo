namespace Upkilo.Core.Interfaces;

public interface IImageGenerationAgent
{
    Task<string> GenerateMarketingImageAsync(Guid tenantId, string businessName, string serviceName, string aesthetic);
    Task<string> GenerateSocialPostImageAsync(Guid tenantId, string platform, string topic);
}
