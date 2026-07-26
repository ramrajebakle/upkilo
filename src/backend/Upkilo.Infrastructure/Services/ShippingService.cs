using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Upkilo.Infrastructure.Services;

public class ShipmentDetails
{
    public string FromZip { get; set; } = string.Empty;
    public string ToZip { get; set; } = string.Empty;
    public double WeightLbs { get; set; }
    public double LengthIn { get; set; }
    public double WidthIn { get; set; }
    public double HeightIn { get; set; }
    public string RecipientName { get; set; } = string.Empty;
    public string RecipientAddress { get; set; } = string.Empty;
    public string RecipientCity { get; set; } = string.Empty;
    public string RecipientState { get; set; } = string.Empty;
    public string RecipientCountry { get; set; } = "US";
}

public class ShippingQuote
{
    public string Carrier { get; set; } = string.Empty;
    public string Service { get; set; } = string.Empty;
    public decimal EstimatedCost { get; set; }
    public int EstimatedDays { get; set; }
    public string? TrackingNumber { get; set; }
}

/// <summary>
/// FedEx REST API v1 integration for rate quotes and shipment creation.
/// </summary>
public class ShippingService
{
    private const string FedExBaseUrl = "https://apis.fedex.com";
    private const string TokenCacheKey = "shipping:fedex:token";

    private readonly ILogger<ShippingService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConnectionMultiplexer _redis;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _accountNumber;

    public ShippingService(
        ILogger<ShippingService> logger,
        IHttpClientFactory httpClientFactory,
        IConnectionMultiplexer redis,
        IConfiguration configuration)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _redis = redis;
        _clientId = configuration["Shipping:FedEx:ClientId"] ?? string.Empty;
        _clientSecret = configuration["Shipping:FedEx:ClientSecret"] ?? string.Empty;
        _accountNumber = configuration["Shipping:FedEx:AccountNumber"] ?? string.Empty;
    }

    private async Task<string> GetAccessTokenAsync()
    {
        var db = _redis.GetDatabase();
        var cached = await db.StringGetAsync(TokenCacheKey);
        if (cached.HasValue) return cached.ToString();

        var client = _httpClientFactory.CreateClient();
        var form = new FormUrlEncodedContent(new[]
        {
            new System.Collections.Generic.KeyValuePair<string, string>("grant_type", "client_credentials"),
            new System.Collections.Generic.KeyValuePair<string, string>("client_id", _clientId),
            new System.Collections.Generic.KeyValuePair<string, string>("client_secret", _clientSecret)
        });

        var resp = await client.PostAsync($"{FedExBaseUrl}/oauth/token", form);
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var token = doc.RootElement.GetProperty("access_token").GetString()!;
        var expiresIn = doc.RootElement.TryGetProperty("expires_in", out var expEl) ? expEl.GetInt32() : 3600;

        await db.StringSetAsync(TokenCacheKey, token, TimeSpan.FromSeconds(expiresIn - 60));
        return token;
    }

    public async Task<ShippingQuote> GetRateQuoteAsync(ShipmentDetails details)
    {
        _logger.LogInformation("Requesting FedEx rate quote from {From} to {To}", details.FromZip, details.ToZip);

        var token = await GetAccessTokenAsync();
        var payload = BuildRatePayload(details);

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var resp = await client.PostAsync($"{FedExBaseUrl}/rate/v1/rates/quotes", content);

        if (!resp.IsSuccessStatusCode)
        {
            var errBody = await resp.Content.ReadAsStringAsync();
            _logger.LogError("FedEx rate quote failed: {Status} {Body}", resp.StatusCode, errBody);
            return new ShippingQuote { Carrier = "FedEx", Service = "Unknown", EstimatedCost = 0 };
        }

        var json = await resp.Content.ReadAsStringAsync();
        return ParseRateResponse(json);
    }

    public async Task<ShippingQuote> CreateShipmentAsync(ShipmentDetails details)
    {
        _logger.LogInformation("Creating FedEx shipment from {From} to {To}", details.FromZip, details.ToZip);

        var token = await GetAccessTokenAsync();
        var payload = BuildShipmentPayload(details);

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var resp = await client.PostAsync($"{FedExBaseUrl}/ship/v1/shipments", content);

        if (!resp.IsSuccessStatusCode)
        {
            var errBody = await resp.Content.ReadAsStringAsync();
            _logger.LogError("FedEx create shipment failed: {Status} {Body}", resp.StatusCode, errBody);
            return new ShippingQuote { Carrier = "FedEx", Service = "Unknown", EstimatedCost = 0 };
        }

        var json = await resp.Content.ReadAsStringAsync();
        return ParseShipmentResponse(json);
    }

    private object BuildRatePayload(ShipmentDetails d) => new
    {
        accountNumber = new { value = _accountNumber },
        requestedShipment = new
        {
            shipper = new { address = new { postalCode = d.FromZip, countryCode = "US" } },
            recipient = new { address = new { postalCode = d.ToZip, countryCode = "US" } },
            pickupType = "USE_SCHEDULED_PICKUP",
            rateRequestType = new[] { "ACCOUNT" },
            requestedPackageLineItems = new[]
            {
                new
                {
                    weight = new { units = "LB", value = d.WeightLbs },
                    dimensions = new { length = (int)d.LengthIn, width = (int)d.WidthIn, height = (int)d.HeightIn, units = "IN" }
                }
            }
        }
    };

    private object BuildShipmentPayload(ShipmentDetails d) => new
    {
        labelResponseOptions = "URL_ONLY",
        requestedShipment = new
        {
            shipper = new
            {
                contact = new { personName = "Sender", phoneNumber = "0000000000" },
                address = new { postalCode = d.FromZip, countryCode = "US" }
            },
            recipients = new[]
            {
                new
                {
                    contact = new { personName = d.RecipientName, phoneNumber = "0000000000" },
                    address = new
                    {
                        streetLines = new[] { d.RecipientAddress },
                        city = d.RecipientCity,
                        stateOrProvinceCode = d.RecipientState,
                        postalCode = d.ToZip,
                        countryCode = d.RecipientCountry
                    }
                }
            },
            pickupType = "USE_SCHEDULED_PICKUP",
            serviceType = "FEDEX_GROUND",
            packagingType = "YOUR_PACKAGING",
            shippingChargesPayment = new
            {
                paymentType = "SENDER",
                payor = new { responsibleParty = new { accountNumber = new { value = _accountNumber } } }
            },
            labelSpecification = new { labelFormatType = "COMMON2D", imageType = "PDF" },
            requestedPackageLineItems = new[]
            {
                new
                {
                    weight = new { units = "LB", value = d.WeightLbs },
                    dimensions = new { length = (int)d.LengthIn, width = (int)d.WidthIn, height = (int)d.HeightIn, units = "IN" }
                }
            }
        },
        accountNumber = new { value = _accountNumber }
    };

    private static ShippingQuote ParseRateResponse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        try
        {
            var details = doc.RootElement
                .GetProperty("output")
                .GetProperty("rateReplyDetails")[0];

            var service = details.TryGetProperty("serviceType", out var svc) ? svc.GetString() ?? "FEDEX_GROUND" : "FEDEX_GROUND";
            var ratedShipDetails = details.GetProperty("ratedShipmentDetails")[0];
            var cost = ratedShipDetails.GetProperty("totalNetCharge").GetDecimal();
            var transitDays = details.TryGetProperty("commit", out var commit) &&
                              commit.TryGetProperty("transitDays", out var days) ? days.GetInt32() : 5;

            return new ShippingQuote { Carrier = "FedEx", Service = service, EstimatedCost = cost, EstimatedDays = transitDays };
        }
        catch
        {
            return new ShippingQuote { Carrier = "FedEx", Service = "FEDEX_GROUND", EstimatedCost = 0 };
        }
    }

    private static ShippingQuote ParseShipmentResponse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        try
        {
            var pieceResponse = doc.RootElement
                .GetProperty("output")
                .GetProperty("transactionShipments")[0]
                .GetProperty("pieceResponses")[0];

            var trackingNumber = pieceResponse.TryGetProperty("trackingNumber", out var tn) ? tn.GetString() : null;

            return new ShippingQuote
            {
                Carrier = "FedEx",
                Service = "FEDEX_GROUND",
                EstimatedCost = 0,
                TrackingNumber = trackingNumber
            };
        }
        catch
        {
            return new ShippingQuote { Carrier = "FedEx", Service = "FEDEX_GROUND", EstimatedCost = 0 };
        }
    }
}
