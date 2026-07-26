using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Interfaces;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// Razorpay integration service for creating orders and capturing payments.
/// Supports REST API.
/// </summary>
public class RazorpayService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RazorpayService> _logger;
    private readonly ISecretProvider _secretProvider;

    public RazorpayService(
        HttpClient httpClient, 
        IConfiguration configuration, 
        ILogger<RazorpayService> logger,
        ISecretProvider secretProvider)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://api.razorpay.com/v1/");
        _configuration = configuration;
        _logger = logger;
        _secretProvider = secretProvider;
    }

    /// <summary>
    /// Creates an order in Razorpay (must be done from server-side before client checkout).
    /// </summary>
    public async Task<string?> CreateOrderAsync(decimal amount, string currency, string receiptId)
    {
        try
        {
            var keyId = _secretProvider.GetSecret("Razorpay--KeyId");
            var keySecret = _secretProvider.GetSecret("Razorpay--KeySecret");

            if (string.IsNullOrEmpty(keyId) || string.IsNullOrEmpty(keySecret))
            {
                throw new InvalidOperationException("Razorpay credentials not configured.");
            }

            var authHeader = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{keyId}:{keySecret}"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeader);

            // Razorpay amount is in the smallest currency unit (e.g. paise). The exponent comes
            // from the currency rather than a flat *100 — Razorpay supports currencies with no
            // minor unit, where scaling by 100 charges 100x.
            var amountInSmallestUnit = Upkilo.Core.Helpers.Currency.ToMinorUnits(amount, currency);

            var payload = new
            {
                amount = amountInSmallestUnit,
                currency = currency.ToUpperInvariant(),
                receipt = receiptId
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("orders", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorStr = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to create Razorpay order. Status: {Status}, Error: {Error}", response.StatusCode, errorStr);
                return null;
            }

            var result = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(result);
            
            return doc.RootElement.GetProperty("id").GetString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Razorpay order for receipt {ReceiptId}", receiptId);
            return null;
        }
    }

    public bool VerifySignature(string orderId, string paymentId, string signature)
    {
        try
        {
            var keySecret = _secretProvider.GetSecret("Razorpay--KeySecret");
            if (string.IsNullOrEmpty(keySecret)) return false;

            var payload = orderId + "|" + paymentId;
            var secretBytes = Encoding.ASCII.GetBytes(keySecret);
            var payloadBytes = Encoding.ASCII.GetBytes(payload);

            using var hmac = new System.Security.Cryptography.HMACSHA256(secretBytes);
            var hashArray = hmac.ComputeHash(payloadBytes);
            var generatedSignature = BitConverter.ToString(hashArray).Replace("-", "").ToLower();

            return generatedSignature == signature.ToLower();
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> CapturePaymentAsync(string paymentId, decimal amount, string currency = "INR")
    {
        try
        {
            var keyId = _secretProvider.GetSecret("Razorpay--KeyId");
            var keySecret = _secretProvider.GetSecret("Razorpay--KeySecret");
            var authHeader = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{keyId}:{keySecret}"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeader);

            var amountInSmallestUnit = Upkilo.Core.Helpers.Currency.ToMinorUnits(amount, currency);
            var payload = new { amount = amountInSmallestUnit, currency = currency.ToUpperInvariant() };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"payments/{paymentId}/capture", content);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to capture Razorpay payment {PaymentId}", paymentId);
            return false;
        }
    }
}
