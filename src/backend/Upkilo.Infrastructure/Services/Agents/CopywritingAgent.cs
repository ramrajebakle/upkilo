using Upkilo.Core.Interfaces;

namespace Upkilo.Infrastructure.Services.Agents;

public class CopywritingAgent : ICopywritingAgent
{
    private readonly IAIService _aiService;

    public CopywritingAgent(IAIService aiService)
    {
        _aiService = aiService;
    }

    public async Task<string> GenerateEmailContentAsync(Guid tenantId, string businessName, string serviceName, string targetAudience, string goal)
    {
        var prompt = $"Act as a professional marketing copywriter. Write a persuasive email for {businessName} promoting their {serviceName} service. " +
                     $"The target audience is {targetAudience} and the goal is {goal}. " +
                     "Keep the tone professional yet inviting. Include a clear call to action.";

        var result = await _aiService.GenerateTextAsync(tenantId, null, prompt);
        return result.Success ? result.Content ?? "" : "Failed to generate email content.";
    }

    public async Task<string> GenerateSmsContentAsync(Guid tenantId, string businessName, string serviceName, string goal)
    {
        var prompt = $"Write a short, punchy SMS marketing message for {businessName} promoting {serviceName}. " +
                     $"Goal: {goal}. Keep it under 160 characters. Must include a sense of urgency.";

        var result = await _aiService.GenerateTextAsync(tenantId, null, prompt, "gpt-3.5-turbo");
        return result.Success ? result.Content ?? "" : "Failed to generate SMS content.";
    }

    public async Task<string> GenerateSocialMediaPostAsync(Guid tenantId, string platform, string businessName, string serviceName, string topic)
    {
        var prompt = $"Create a compelling {platform} post for {businessName} about {topic} related to their {serviceName} service. " +
                     "Include relevant hashtags and an engaging hook.";

        var result = await _aiService.GenerateTextAsync(tenantId, null, prompt);
        return result.Success ? result.Content ?? "" : "Failed to generate social media post.";
    }
}
