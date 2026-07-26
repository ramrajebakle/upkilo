using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Upkilo.AI.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace Upkilo.AI.Services;

// DEPRECATED: Not registered in DI. IAIService is wired to AzureOpenAIService (Program.cs:392).
// Do not use directly — calls api.openai.com without Azure commitment discounts or usage tracking.
[Obsolete("Use AzureOpenAIService via IAIService injection instead.")]
public class OpenAIService : Upkilo.AI.Interfaces.IAIService
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public OpenAIService(IConfiguration configuration, HttpClient httpClient)
    {
        _configuration = configuration;
        _httpClient = httpClient;
    }

    public async Task<string> GeneralQueryAsync(string prompt, string? model = null)
    {
        var apiKey = _configuration["AI:OpenAI:ApiKey"];
        if (string.IsNullOrEmpty(apiKey)) return "OpenAI API Key not configured.";

        var requestBody = new
        {
            model = model ?? "gpt-4",
            messages = new[] { new { role = "user", content = prompt } }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
        request.Headers.Add("Authorization", $"Bearer {apiKey}");
        request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode) return $"Error: {response.StatusCode}";

        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? string.Empty;
    }

    public async Task<string> AnalyzeTextAsync(string text, string instruction)
    {
        return await GeneralQueryAsync($"{instruction}\n\nText: {text}");
    }

    public async Task<IEnumerable<float>> GenerateEmbeddingsAsync(string text)
    {
        // Implementation for OpenAI Embeddings API
        return new List<float>();
    }
}
