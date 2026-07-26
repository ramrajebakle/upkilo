using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Upkilo.Infrastructure.Services;

public class DomainManagementService
{
    private readonly ILogger<DomainManagementService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public DomainManagementService(
        ILogger<DomainManagementService> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public async Task<DnsVerificationResult> VerifyDomainAsync(string domain, string expectedTxtValue)
    {
        _logger.LogInformation("Verifying DNS TXT record for domain {Domain}", domain);

        try
        {
            var http = _httpClientFactory.CreateClient();
            http.DefaultRequestHeaders.Add("Accept", "application/dns-json");
            var dohUrl = $"https://cloudflare-dns.com/dns-query?name={Uri.EscapeDataString(domain)}&type=TXT";
            var response = await http.GetAsync(dohUrl);

            if (!response.IsSuccessStatusCode)
                return new DnsVerificationResult
                {
                    IsVerified = false,
                    Error = $"DNS query failed: {response.StatusCode}",
                    LastChecked = DateTime.UtcNow
                };

            var json = await response.Content.ReadAsStringAsync();
            var expectedRecord = $"upkilo-verification={expectedTxtValue}";
            var isVerified = json.Contains(expectedRecord, StringComparison.OrdinalIgnoreCase);

            _logger.LogInformation("DNS TXT verification for {Domain}: {Result}", domain, isVerified ? "verified" : "not found");
            return new DnsVerificationResult { IsVerified = isVerified, LastChecked = DateTime.UtcNow };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DNS verification failed for {Domain}", domain);
            return new DnsVerificationResult { IsVerified = false, Error = ex.Message };
        }
    }

    public async Task ProvisionCertificateAsync(string domain)
    {
        _logger.LogInformation("Provisioning managed SSL certificate for {Domain}", domain);

        var subscriptionId = _configuration["Azure:SubscriptionId"];
        var resourceGroup = _configuration["Azure:ResourceGroup"];
        var appName = _configuration["Azure:AppServiceName"];

        if (string.IsNullOrEmpty(subscriptionId) || string.IsNullOrEmpty(resourceGroup) || string.IsNullOrEmpty(appName))
        {
            _logger.LogWarning("Azure App Service config missing — cannot provision certificate for {Domain}", domain);
            return;
        }

        try
        {
            // Obtain Azure access token via managed identity / service principal
            var credential = new DefaultAzureCredential();
            var tokenRequestCtx = new TokenRequestContext(new[] { "https://management.azure.com/.default" });
            var token = await credential.GetTokenAsync(tokenRequestCtx);

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Token);

            // Step 1: Add custom hostname binding to App Service
            var bindingUrl = $"https://management.azure.com/subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}" +
                             $"/providers/Microsoft.Web/sites/{appName}/hostNameBindings/{domain}?api-version=2022-03-01";

            var bindingBody = JsonSerializer.Serialize(new
            {
                properties = new { siteName = appName, hostNameType = "Verified", sslState = "Disabled" }
            });

            var bindingResponse = await client.PutAsync(bindingUrl, new StringContent(bindingBody, Encoding.UTF8, "application/json"));
            if (!bindingResponse.IsSuccessStatusCode)
            {
                var err = await bindingResponse.Content.ReadAsStringAsync();
                _logger.LogError("Failed to add hostname binding for {Domain}: {Error}", domain, err);
                return;
            }

            // Step 2: Create App Service Managed Certificate
            var certUrl = $"https://management.azure.com/subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}" +
                          $"/providers/Microsoft.Web/certificates/{domain}-cert?api-version=2022-03-01";

            var certBody = JsonSerializer.Serialize(new
            {
                location = _configuration["Azure:Location"] ?? "eastus",
                properties = new
                {
                    serverFarmId = $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/providers/Microsoft.Web/serverfarms/{appName}-plan",
                    canonicalName = domain
                }
            });

            var certResponse = await client.PutAsync(certUrl, new StringContent(certBody, Encoding.UTF8, "application/json"));
            if (!certResponse.IsSuccessStatusCode)
            {
                var err = await certResponse.Content.ReadAsStringAsync();
                _logger.LogError("Failed to create managed certificate for {Domain}: {Error}", domain, err);
                return;
            }

            _logger.LogInformation("Managed SSL certificate provisioned for {Domain}", domain);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Certificate provisioning failed for {Domain}", domain);
            throw;
        }
    }
}

public class DnsVerificationResult
{
    public bool IsVerified { get; set; }
    public DateTime LastChecked { get; set; }
    public string? Error { get; set; }
}
