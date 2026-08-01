using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Controllers;

/// <summary>
/// G1-G5: Global Payments gateway routing.
/// Routes payment initiation to region-appropriate methods:
///   G1 India    — UPI (PhonePe/Razorpay), DPDP compliance
///   G2 SEA      — GrabPay, GoPay, PromptPay (Thailand/Singapore)
///   G3 MENA     — STC Pay, Mada, Fawry
///   G4 LATAM    — Pix (Brazil), Mercado Pago
///   G5 Japan    — PayPay, konbini
/// All methods produce a payment intent that the frontend resolves.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/global-payments")]
[Authorize]
public class GlobalPaymentsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<GlobalPaymentsController> _logger;

    // Supported regional payment methods by ISO 3166-1 alpha-2 country code
    private static readonly Dictionary<string, RegionalPaymentConfig> RegionalConfigs = new(StringComparer.OrdinalIgnoreCase)
    {
        ["IN"] = new("India", new[] { "upi", "razorpay", "paytm" }, "INR", "DPDP_2023", "en-IN"),
        ["TH"] = new("Thailand", new[] { "promptpay", "truemoney", "rabbit_linepay" }, "THB", "PDPA", "th-TH"),
        ["SG"] = new("Singapore", new[] { "grabpay", "paynow", "nets" }, "SGD", "PDPA", "en-SG"),
        ["ID"] = new("Indonesia", new[] { "gopay", "ovo", "dana", "bca" }, "IDR", "UU_PDP", "id-ID"),
        ["SA"] = new("Saudi Arabia", new[] { "stcpay", "mada", "apple_pay" }, "SAR", "SAMA", "ar-SA"),
        ["AE"] = new("UAE", new[] { "payfort", "tabby", "apple_pay" }, "AED", "CBUAE", "ar-AE"),
        ["EG"] = new("Egypt", new[] { "fawry", "meeza", "vodafone_cash" }, "EGP", "NCBE", "ar-EG"),
        ["BR"] = new("Brazil", new[] { "pix", "mercadopago", "boleto" }, "BRL", "LGPD", "pt-BR"),
        ["MX"] = new("Mexico", new[] { "mercadopago", "oxxo", "spei" }, "MXN", "LFPDPPP", "es-MX"),
        ["JP"] = new("Japan", new[] { "paypay", "konbini", "suica", "linepay" }, "JPY", "APPI", "ja-JP"),
    };

    public GlobalPaymentsController(AppDbContext context, ITenantProvider tenantProvider, ILogger<GlobalPaymentsController> logger)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    /// <summary>
    /// GET /global-payments/methods?countryCode=IN — Returns available payment methods for a country.
    /// </summary>
    [HttpGet("methods")]
    [AllowAnonymous]
    public IActionResult GetPaymentMethods([FromQuery] string countryCode)
    {
        if (!RegionalConfigs.TryGetValue(countryCode, out var config))
        {
            return Ok(new
            {
                countryCode = countryCode.ToUpper(),
                methods = new[] { "stripe_card", "apple_pay", "google_pay" },
                currency = "USD",
                locale = "en-US",
                compliance = "GDPR",
                note = "Defaulting to global Stripe methods. Contact support to request regional payment methods for your country."
            });
        }

        return Ok(new
        {
            countryCode = countryCode.ToUpper(),
            country = config.CountryName,
            methods = config.Methods,
            currency = config.Currency,
            locale = config.Locale,
            compliance = config.ComplianceFramework,
            rtl = config.Locale.StartsWith("ar"),
            currencySymbol = GetCurrencySymbol(config.Currency)
        });
    }

    /// <summary>
    /// POST /global-payments/initiate — Creates a payment intent for the appropriate regional method.
    /// Returns provider-specific data the frontend SDK uses to complete payment.
    /// </summary>
    [HttpPost("initiate")]
    public async Task<IActionResult> InitiateGlobalPayment([FromBody] GlobalPaymentRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        if (!RegionalConfigs.TryGetValue(request.CountryCode, out var config))
            return BadRequest(new { error = "unsupported_country", supportedCountries = RegionalConfigs.Keys });

        if (!config.Methods.Contains(request.PaymentMethod, StringComparer.OrdinalIgnoreCase))
            return BadRequest(new
            {
                error = "unsupported_method",
                message = $"'{request.PaymentMethod}' is not supported in {config.CountryName}.",
                availableMethods = config.Methods
            });

        // Generate a provider-specific intent
        var intentId = $"pi_{request.PaymentMethod.ToUpper()}_{Guid.NewGuid():N}"[..32];
        var expiresAt = DateTime.UtcNow.AddMinutes(15);

        _logger.LogInformation("[GlobalPayments] G{Region} payment initiated: method={Method} amount={Amount}{Currency} tenant={TenantId}",
            GetRegionCode(request.CountryCode), request.PaymentMethod, request.Amount, config.Currency, tenantId);

        // Build provider-specific redirect data
        var providerData = request.PaymentMethod.ToLower() switch
        {
            "upi" => (object)new
            {
                upiIntent = $"upi://pay?pa=upkilo@razorpay&pn=Upkilo&am={request.Amount}&cu={config.Currency}&tn={request.Description}",
                qrCodeUrl = $"https://api.upkilo.com/qr/{intentId}",
                deepLink = $"razorpay://checkout/{intentId}"
            },
            "pix" => new
            {
                pixKey = "upkilo@pix.com.br",
                qrCode = $"00020126520014br.gov.bcb.pix0136{intentId}",
                pixCopyPaste = $"00020126580014br.gov.bcb.pix0136{intentId}5204000053039865802BR5913Upkilo6009SAO PAULO62070503***6304{intentId[..4].ToUpper()}"
            },
            "promptpay" => new
            {
                promptPayId = "0105551234567",
                qrCodeUrl = $"https://api.upkilo.com/qr/promptpay/{intentId}",
                amount = request.Amount
            },
            "konbini" => new
            {
                convenienceStore = "FamilyMart",
                confirmationNumber = $"K{Guid.NewGuid():N}"[..12].ToUpper(),
                barcode = intentId,
                instructions = "Show this barcode at any FamilyMart, Lawson, or 7-Eleven in Japan.",
                payByDate = expiresAt.AddDays(3).ToString("yyyy-MM-dd")
            },
            _ => new
            {
                redirectUrl = $"https://checkout.upkilo.com/{request.PaymentMethod}/{intentId}",
                returnUrl = request.ReturnUrl ?? "https://app.upkilo.com/booking/confirm"
            }
        };

        return Ok(new
        {
            intentId,
            paymentMethod = request.PaymentMethod,
            amount = request.Amount,
            currency = config.Currency,
            status = "pending",
            expiresAt,
            providerData,
            compliance = new
            {
                framework = config.ComplianceFramework,
                dataLocalization = request.CountryCode is "IN" or "SA" or "AE",
                requiresConsent = request.CountryCode is "TH" or "SG" or "BR"
            }
        });
    }

    /// <summary>
    /// GET /global-payments/compliance/{countryCode} — Returns compliance requirements for a country.
    /// G1=DPDP, G2=PDPA, G3=SAMA, G4=LGPD, G5=APPI.
    /// </summary>
    [HttpGet("compliance/{countryCode}")]
    [AllowAnonymous]
    public IActionResult GetComplianceInfo(string countryCode)
    {
        var info = countryCode.ToUpper() switch
        {
            "IN" => new ComplianceInfo("DPDP Bill 2023", "India", "Data must be stored in India. User consent required for processing. Right to correction/erasure.", new[] { "Data localization", "Consent management", "Right to erasure", "Grievance officer required" }),
            "TH" or "SG" or "ID" => new ComplianceInfo("PDPA", "Southeast Asia", "Personal data processing requires explicit consent. Cross-border transfers require adequacy decision.", new[] { "Explicit consent", "Data subject rights", "DPO appointment", "72h breach notification" }),
            "SA" or "AE" => new ComplianceInfo("SAMA / CBUAE", "MENA", "Payment data must be stored in-country. Saudi Central Bank and UAE Central Bank regulations apply.", new[] { "In-country data storage", "PCI-DSS Level 1", "Anti-money laundering checks", "Transaction monitoring" }),
            "BR" or "MX" => new ComplianceInfo("LGPD / LFPDPPP", "LATAM", "Brazilian GDPR equivalent. Data processing basis required. DPA authority is ANPD.", new[] { "Processing basis documentation", "ANPD registration (Brazil)", "Right to portability", "Privacy notice" }),
            "JP" => new ComplianceInfo("APPI", "Japan", "Act on Protection of Personal Information. Third-party provision requires opt-in consent.", new[] { "Opt-in consent for third-party sharing", "Security management measures", "Annual self-assessment", "Foreign transfer safeguards" }),
            _ => new ComplianceInfo("GDPR", "Global", "European GDPR applies as default for EU/EEA residents.", new[] { "Lawful basis for processing", "Privacy notice", "Data subject rights", "72h breach notification" })
        };

        return Ok(info);
    }

    private static string GetRegionCode(string countryCode) => countryCode.ToUpper() switch
    {
        "IN" => "1 (India)",
        "TH" or "SG" or "ID" => "2 (SEA)",
        "SA" or "AE" or "EG" => "3 (MENA)",
        "BR" or "MX" => "4 (LATAM)",
        "JP" => "5 (Japan)",
        _ => "0 (Global)"
    };

    private static string GetCurrencySymbol(string code) => code switch
    {
        "INR" => "₹",
        "THB" => "฿",
        "SGD" => "S$",
        "IDR" => "Rp",
        "SAR" => "﷼",
        "AED" => "د.إ",
        "EGP" => "E£",
        "BRL" => "R$",
        "MXN" => "MX$",
        "JPY" => "¥",
        _ => "$"
    };
}

public record RegionalPaymentConfig(
    string CountryName,
    string[] Methods,
    string Currency,
    string ComplianceFramework,
    string Locale);

public record ComplianceInfo(
    string Framework,
    string Region,
    string Summary,
    string[] Requirements);

public class GlobalPaymentRequest
{
    public string CountryCode { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public string? ReturnUrl { get; set; }
}
