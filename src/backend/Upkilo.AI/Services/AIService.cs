using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Upkilo.AI.Interfaces;
using Azure.AI.OpenAI;
using OpenAI.Chat;
using OpenAI.Embeddings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Upkilo.AI.Services;

public class AIService : Upkilo.AI.Interfaces.IAIService
{
    private readonly AzureOpenAIClient? _client;
    private readonly ILogger<AIService> _logger;
    private readonly string _defaultModel = "gpt-4o-mini";
    private readonly bool _isConfigured;

    public AIService(IConfiguration configuration, ILogger<AIService> logger)
    {
        _logger = logger;

        var endpoint = configuration["Azure:OpenAI:Endpoint"];
        var key = configuration["Azure:OpenAI:Key"];
        _defaultModel = configuration["Azure:OpenAI:DefaultModel"] ?? "gpt-4o-mini";

        _isConfigured = !string.IsNullOrEmpty(endpoint) && !string.IsNullOrEmpty(key);

        if (_isConfigured)
        {
            _client = new AzureOpenAIClient(new Uri(endpoint!), new System.ClientModel.ApiKeyCredential(key!));
            _logger.LogInformation("Azure OpenAI service initialized with endpoint {Endpoint}", endpoint);
        }
        else
        {
            _logger.LogWarning("Azure OpenAI is not configured. AI operations will return degraded fallback responses.");
        }
    }

    public async Task<string> GeneralQueryAsync(string prompt, string? model = null)
    {
        if (!_isConfigured || _client == null)
            return $"[AI Degraded] Could not process query: {prompt}";

        try
        {
            var chatClient = _client.GetChatClient(model ?? _defaultModel);
            var response = await chatClient.CompleteChatAsync(prompt);
            return response.Value.Content[0].Text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query AI for prompt {Prompt}", prompt);
            return $"[AI Error] Something went wrong.";
        }
    }

    public async Task<string> AnalyzeTextAsync(string text, string instruction)
    {
        if (!_isConfigured || _client == null)
            return $"[AI Degraded] Could not analyze text.";

        try
        {
            var chatClient = _client.GetChatClient(_defaultModel);
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage($"You are an expert system. Instruction: {instruction}"),
                new UserChatMessage($"Analyze the following text: {text}")
            };

            var response = await chatClient.CompleteChatAsync(messages);
            return response.Value.Content[0].Text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze text with AI");
            return $"[AI Error] Analysis failed.";
        }
    }

    public async Task<IEnumerable<float>> GenerateEmbeddingsAsync(string text)
    {
        if (!_isConfigured || _client == null)
            return new float[] { 0 };

        try
        {
            var embeddingClient = _client.GetEmbeddingClient("text-embedding-3-small");
            var response = await embeddingClient.GenerateEmbeddingAsync(text);
            var floatReadOnlyMemory = response.Value.ToFloats();
            return floatReadOnlyMemory.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate embeddings");
            return new float[] { 0 };
        }
    }
}
