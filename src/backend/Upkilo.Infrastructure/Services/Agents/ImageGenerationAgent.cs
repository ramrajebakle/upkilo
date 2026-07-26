using Upkilo.Core.Interfaces;

namespace Upkilo.Infrastructure.Services.Agents;

public class ImageGenerationAgent : IImageGenerationAgent
{
    private readonly IAIService _aiService;

    public ImageGenerationAgent(IAIService aiService)
    {
        _aiService = aiService;
    }

    public async Task<string> GenerateMarketingImageAsync(Guid tenantId, string businessName, string serviceName, string aesthetic)
    {
        var prompt = $"Create a high-quality, {aesthetic} marketing image for {businessName} showcasing their {serviceName} service. " +
                     "The image should be professional, visually appealing, and suitable for a high-end booking platform.";

        var result = await _aiService.GenerateImageAsync(tenantId, null, prompt);
        return result.Success ? result.ImageUrl ?? "" : "Failed to generate marketing image.";
    }

    public async Task<string> GenerateSocialPostImageAsync(Guid tenantId, string platform, string topic)
    {
        var prompt = $"Generate an engaging social media image for {platform} about {topic}. " +
                     "The design should be modern, eye-catching, and optimized for social media engagement.";

        var result = await _aiService.GenerateImageAsync(tenantId, null, prompt);
        return result.Success ? result.ImageUrl ?? "" : "Failed to generate social media post image.";
    }
}
