using Upkilo.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Upkilo.Infrastructure.Agents;

/// <summary>
/// AI Agent specialized in generating marketing copywriting content.
/// </summary>
public class CopywritingAgent : ICopywritingAgent
{
    private readonly IAIService _aiService;
    private readonly ILogger<CopywritingAgent> _logger;

    public CopywritingAgent(IAIService aiService, ILogger<CopywritingAgent> logger)
    {
        _aiService = aiService;
        _logger = logger;
    }

    public async Task<string> GenerateEmailContentAsync(Guid tenantId, string businessName, string serviceName, string targetAudience, string goal)
    {
        var prompt = $"Write a professional marketing email for {businessName}. " +
                     $"The service is {serviceName}. " +
                     $"The target audience is {targetAudience}. " +
                     $"The goal of the email is: {goal}. " +
                     "Keep it engaging and concise.";

        var result = await _aiService.GenerateTextAsync(tenantId, null, prompt);
        return result.Success ? result.Content ?? "" : "Failed to generate content: " + result.Error;
    }

    public async Task<string> GenerateSmsContentAsync(Guid tenantId, string businessName, string serviceName, string goal)
    {
        var prompt = $"Write a short SMS marketing message for {businessName}. " +
                     $"Service: {serviceName}. Goal: {goal}. Max 160 characters.";

        var result = await _aiService.GenerateTextAsync(tenantId, null, prompt, "gpt-3.5-turbo");
        return result.Success ? result.Content ?? "" : "Failed to generate content: " + result.Error;
    }

    public async Task<string> GenerateSocialMediaPostAsync(Guid tenantId, string platform, string businessName, string serviceName, string topic)
    {
        var prompt = $"Create a {platform} post for {businessName}. " +
                     $"Topic: {topic}. Related to service: {serviceName}. " +
                     "Include relevant hashtags.";

        var result = await _aiService.GenerateTextAsync(tenantId, null, prompt);
        return result.Success ? result.Content ?? "" : "Failed to generate content: " + result.Error;
    }
}
