using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Upkilo.Infrastructure.Services
{
    public class WebhookV2Service : IWebhookV2Service
    {
        private readonly AppDbContext _context;
        private readonly ILogger<WebhookV2Service> _logger;
        private readonly HttpClient _httpClient;

        public WebhookV2Service(AppDbContext context, ILogger<WebhookV2Service> logger, HttpClient httpClient)
        {
            _context = context;
            _logger = logger;
            _httpClient = httpClient;
        }

        public async Task PublishEventAsync<T>(string eventType, T payload, Guid? tenantId = null) where T : class
        {
            _logger.LogInformation("Publishing Webhook V2 event {EventType} for tenant {TenantId}", eventType, tenantId);

            var payloadJson = JsonSerializer.Serialize(new
            {
                @event = eventType,
                timestamp = DateTime.UtcNow,
                data = payload
            });

            if (tenantId == null)
            {
                _logger.LogWarning("Cannot publish Webhook V2 event without a tenantId.");
                return;
            }

            var endpoints = await _context.Webhooks
                .Where(w => w.TenantId == tenantId.Value && w.IsActive && !w.IsDeleted)
                .ToListAsync();

            var relevantEndpoints = endpoints.Where(w => w.Events.Contains(eventType)).ToList();

            if (!relevantEndpoints.Any())
            {
                _logger.LogInformation("No registered endpoints found for event type {EventType}.", eventType);
                return;
            }

            foreach (var endpoint in relevantEndpoints)
            {
                var signature = ComputeSignature(payloadJson, endpoint.Secret);
                // In production, queue this to a background worker to avoid blocking
                _ = SendWebhookAsync(endpoint.Url, payloadJson, signature, endpoint.Id);
            }
        }

        private async Task SendWebhookAsync(string url, string payload, string signature, Guid webhookId)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            bool success = false;
            int? statusCode = null;
            string? responseBody = null;
            string? errorMsg = null;

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
                request.Headers.Add("X-Upkilo-Signature", signature);

                var response = await _httpClient.SendAsync(request);
                statusCode = (int)response.StatusCode;
                responseBody = await response.Content.ReadAsStringAsync();

                if (responseBody.Length > 1000) responseBody = responseBody.Substring(0, 1000);

                if (response.IsSuccessStatusCode)
                {
                    success = true;
                }
                else
                {
                    errorMsg = $"HTTP {response.StatusCode}";
                    _logger.LogWarning("Webhook delivery failed to {Url} with status {Status}", url, response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                errorMsg = ex.Message;
                _logger.LogError(ex, "Error delivering webhook to {Url}", url);
            }
            finally
            {
                stopwatch.Stop();
                try
                {
                    // Create a new scope / context for logging to avoid threading issues
                    var delivery = new Upkilo.Core.Entities.WebhookDelivery
                    {
                        WebhookId = webhookId,
                        EventType = "Dispatch",
                        Payload = payload,
                        ResponseStatusCode = statusCode,
                        ResponseBody = responseBody,
                        Success = success,
                        Error = errorMsg,
                        DurationMs = (int)stopwatch.ElapsedMilliseconds,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    // Note: since this runs in fire-and-forget, it would normally write to DbContext via a scoped factory.
                    // For now, doing simple log to prevent lifetime exceptions if context is closed.
                    _logger.LogInformation("Webhook Delivery logged: Success={Success}, Status={Status}", success, statusCode);
                }
                catch (Exception logEx)
                {
                    _logger.LogError(logEx, "Failed to log webhook delivery");
                }
            }
        }

        public Task<bool> VerifySignatureAsync(string payload, string signature, string secret)
        {
            var expectedSignature = ComputeSignature(payload, secret);
            return Task.FromResult(expectedSignature == signature);
        }

        private string ComputeSignature(string payload, string secret)
        {
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
            {
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }

        public async Task ProcessRetriesAsync()
        {
            // Logic for retrying failed deliveries in DLQ
            await Task.CompletedTask;
        }
    }
}
