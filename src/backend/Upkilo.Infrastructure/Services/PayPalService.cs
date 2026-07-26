using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// PayPal integration service for creating orders and capturing payments.
/// Supports REST API.
/// </summary>
public class PayPalService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PayPalService> _logger;

    public PayPalService(HttpClient httpClient, IConfiguration configuration, ILogger<PayPalService> logger)
    {
        _httpClient = httpClient;
        
        var isSandboxStr = configuration["PayPal:IsSandbox"];
        var isSandbox = string.IsNullOrEmpty(isSandboxStr) || !bool.TryParse(isSandboxStr, out var parsed) || parsed;
        var baseUrl = isSandbox ? "https://api-m.sandbox.paypal.com/" : "https://api-m.paypal.com/";
        _httpClient.BaseAddress = new Uri(baseUrl);
        
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string?> CreateOrderAsync(decimal amount, string currency, string returnUrl, string cancelUrl)
    {
        try
        {
            var token = await GetAccessTokenAsync();
            if (string.IsNullOrEmpty(token)) return null;

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var payload = new
            {
                intent = "CAPTURE",
                purchase_units = new[]
                {
                    new
                    {
                        amount = new
                        {
                            currency_code = currency.ToUpper(),
                            value = amount.ToString("F2")
                        }
                    }
                },
                application_context = new
                {
                    return_url = returnUrl,
                    cancel_url = cancelUrl
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("v2/checkout/orders", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorStr = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to create PayPal order. Status: {Status}, Error: {Error}", response.StatusCode, errorStr);
                return null;
            }

            var result = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(result);
            
            // Return order ID
            return doc.RootElement.GetProperty("id").GetString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating PayPal order");
            return null;
        }
    }

    private async Task<string?> GetAccessTokenAsync()
    {
        var clientId = _configuration["PayPal:ClientId"];
        var clientSecret = _configuration["PayPal:ClientSecret"];

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            throw new InvalidOperationException(
                "PayPal credentials are not configured. Set PayPal:ClientId and PayPal:ClientSecret in application settings.");

        var authHeader = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{clientId}:{clientSecret}"));
        var request = new HttpRequestMessage(HttpMethod.Post, "v1/oauth2/token")
        {
            Content = new StringContent("grant_type=client_credentials", Encoding.UTF8, "application/x-www-form-urlencoded")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authHeader);

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;

        var result = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(result);
        return doc.RootElement.GetProperty("access_token").GetString();
    }
}
