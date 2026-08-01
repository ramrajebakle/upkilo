using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;

namespace Upkilo.Infrastructure.Services.Integrations;

public interface IIntercomIntegrationService
{
    Task SyncUserAsync(Guid tenantId, User user);
    Task CreateTicketAsync(Guid tenantId, SupportTicket ticket);
}

public class IntercomIntegrationService : IIntercomIntegrationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<IntercomIntegrationService> _logger;
    private readonly ISecretProvider _secretProvider;

    public IntercomIntegrationService(HttpClient httpClient, ILogger<IntercomIntegrationService> logger, ISecretProvider secretProvider)
    {
        _httpClient = httpClient;
        _logger = logger;
        _secretProvider = secretProvider;
        _httpClient.BaseAddress = new Uri("https://api.intercom.io/");
    }

    private async Task<string?> GetAccessToken(Guid tenantId)
    {
        // In a real implementation, we'd look up the OAuth token for this tenant
        // For now, we attempt to get a system-wide or tenant-specific secret
        return _secretProvider.GetSecret($"Intercom--Token--{tenantId}")
            ?? _secretProvider.GetSecret("Intercom--DefaultToken");
    }

    public async Task SyncUserAsync(Guid tenantId, User user)
    {
        var token = await GetAccessToken(tenantId);
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("No Intercom token found for Tenant {TenantId}. Skipping user sync.", tenantId);
            return;
        }

        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        _httpClient.DefaultRequestHeaders.Add("Intercom-Version", "2.10");

        var payload = new
        {
            role = "user",
            external_id = user.Id.ToString(),
            email = user.Email,
            name = $"{user.FirstName} {user.LastName}",
            created_at = ((DateTimeOffset)user.CreatedAt).ToUnixTimeSeconds()
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync("contacts", payload);
            response.EnsureSuccessStatusCode();
            _logger.LogInformation("Successfully synced user {UserId} to Intercom for Tenant {TenantId}", user.Id, tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync user {UserId} to Intercom", user.Id);
        }
    }

    public async Task CreateTicketAsync(Guid tenantId, SupportTicket ticket)
    {
        var token = await GetAccessToken(tenantId);
        if (string.IsNullOrEmpty(token)) return;

        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        _httpClient.DefaultRequestHeaders.Add("Intercom-Version", "2.10");

        var payload = new
        {
            ticket_attributes = new
            {
                title = ticket.Subject,
                description = ticket.Description
            },
            contacts = new[]
            {
                new { id = ticket.SubmittedByUserId.ToString() }
            }
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync("tickets", payload);
            response.EnsureSuccessStatusCode();
            _logger.LogInformation("Successfully created Intercom ticket for SupportTicket {TicketId}", ticket.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Intercom ticket for SupportTicket {TicketId}", ticket.Id);
        }
    }
}
