namespace Upkilo.Core.Helpers;

/// <summary>
/// Metadata for a single currency.
/// </summary>
/// <param name="Code">ISO 4217 alpha-3 code, uppercase.</param>
/// <param name="Symbol">Display symbol.</param>
/// <param name="Exponent">
/// ISO 4217 minor-unit exponent — the number of decimal places. This is the field that
/// matters for payment correctness: JPY is 0, USD is 2, KWD is 3.
/// </param>
/// <param name="Name">English display name.</param>
public sealed record CurrencyInfo(
    string Code,
    string Symbol,
    int Exponent,
    string Name);

/// <summary>
/// Authoritative currency registry.
///
/// Two distinct concerns, deliberately kept separate:
///
///   * <see cref="IsSupported"/> — is this a currency we have catalogued and are willing to
///     persist? Intended for validating input at write boundaries so an unknown code cannot
///     reach the database (and from there a formatter that throws on it).
///     NOTE: the main write paths no longer need it — tenant and service currency are both
///     derived server-side from the tenant's connected Stripe account rather than accepted from
///     a request. It remains the right check for any endpoint that does still take a
///     client-supplied code; EnterpriseController's PlanPrice creation is one such place and is
///     currently unvalidated.
///
///   * <see cref="ToMinorUnits"/> / <see cref="FromMinorUnits"/> — money math. These NEVER
///     throw. An uncatalogued code falls back to 2 decimals, which is correct for the large
///     majority of world currencies. Refusing to compute would turn a display problem into an
///     outage; computing with a sane default degrades gracefully.
/// </summary>
public static class Currency
{
    public const string Default = "USD";

    /// <summary>
    /// Currencies with no minor unit. Sending these to a payment processor scaled by 100
    /// charges the customer 100x the intended amount, so this list is correctness-critical.
    /// Matches Stripe's zero-decimal list.
    /// </summary>
    private static readonly HashSet<string> ZeroDecimal = new(StringComparer.OrdinalIgnoreCase)
    {
        "BIF", "CLP", "DJF", "GNF", "JPY", "KMF", "KRW", "MGA",
        "PYG", "RWF", "UGX", "VND", "VUV", "XAF", "XOF", "XPF"
    };

    /// <summary>
    /// Currencies with three minor units. Stripe additionally requires that amounts in these
    /// currencies be evenly divisible by 10 — see <see cref="ToMinorUnits"/>.
    /// </summary>
    private static readonly HashSet<string> ThreeDecimal = new(StringComparer.OrdinalIgnoreCase)
    {
        "BHD", "JOD", "KWD", "OMR", "TND"
    };

    private static readonly Dictionary<string, CurrencyInfo> Catalog =
        new[]
        {
            // Subscription billing currencies (PlanPrices carries a row per plan for each).
            new CurrencyInfo("USD", "$",    2, "US Dollar"),
            new CurrencyInfo("EUR", "€",    2, "Euro"),
            new CurrencyInfo("GBP", "£",    2, "British Pound"),
            new CurrencyInfo("INR", "₹",    2, "Indian Rupee"),
            new CurrencyInfo("CAD", "CA$",  2, "Canadian Dollar"),
            new CurrencyInfo("AUD", "A$",   2, "Australian Dollar"),
            new CurrencyInfo("AED", "د.إ",  2, "UAE Dirham"),

            // Regional payment currencies — GlobalPaymentsController accepts local payment
            // methods in each of these, so tenants must be able to price in them too.
            new CurrencyInfo("JPY", "¥",    0, "Japanese Yen"),
            new CurrencyInfo("SGD", "S$",   2, "Singapore Dollar"),
            new CurrencyInfo("THB", "฿",    2, "Thai Baht"),
            new CurrencyInfo("IDR", "Rp",   2, "Indonesian Rupiah"),
            new CurrencyInfo("SAR", "﷼",    2, "Saudi Riyal"),
            new CurrencyInfo("EGP", "E£",   2, "Egyptian Pound"),
            new CurrencyInfo("BRL", "R$",   2, "Brazilian Real"),
            new CurrencyInfo("MXN", "MX$",  2, "Mexican Peso"),

            // Common currencies a global tenant base will ask for. Listed so they validate and
            // format correctly rather than being rejected at signup.
            new CurrencyInfo("NZD", "NZ$",  2, "New Zealand Dollar"),
            new CurrencyInfo("CHF", "CHF",  2, "Swiss Franc"),
            new CurrencyInfo("SEK", "kr",   2, "Swedish Krona"),
            new CurrencyInfo("NOK", "kr",   2, "Norwegian Krone"),
            new CurrencyInfo("DKK", "kr",   2, "Danish Krone"),
            new CurrencyInfo("PLN", "zł",   2, "Polish Zloty"),
            new CurrencyInfo("ZAR", "R",    2, "South African Rand"),
            new CurrencyInfo("HKD", "HK$",  2, "Hong Kong Dollar"),
            new CurrencyInfo("MYR", "RM",   2, "Malaysian Ringgit"),
            new CurrencyInfo("PHP", "₱",    2, "Philippine Peso"),
            new CurrencyInfo("TRY", "₺",    2, "Turkish Lira"),
            new CurrencyInfo("KRW", "₩",    0, "South Korean Won"),
            new CurrencyInfo("VND", "₫",    0, "Vietnamese Dong"),
            new CurrencyInfo("NGN", "₦",    2, "Nigerian Naira"),
            new CurrencyInfo("KES", "KSh",  2, "Kenyan Shilling"),
            new CurrencyInfo("QAR", "ر.ق",  2, "Qatari Riyal"),
            new CurrencyInfo("KWD", "د.ك",  3, "Kuwaiti Dinar"),
            new CurrencyInfo("BHD", "ب.د",  3, "Bahraini Dinar"),
        }
        .ToDictionary(c => c.Code, StringComparer.OrdinalIgnoreCase);

    /// <summary>All catalogued currencies, ordered by code.</summary>
    public static IReadOnlyList<CurrencyInfo> All { get; } =
        Catalog.Values.OrderBy(c => c.Code, StringComparer.Ordinal).ToList();

    /// <summary>True when the code is catalogued. Null/blank is false.</summary>
    public static bool IsSupported(string? code) =>
        !string.IsNullOrWhiteSpace(code) && Catalog.ContainsKey(code.Trim());

    /// <summary>
    /// Look up currency metadata, falling back to a synthesized 2-decimal entry for codes we
    /// have not catalogued. Never returns null and never throws.
    /// </summary>
    public static CurrencyInfo Get(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return Catalog[Default];

        var normalized = Normalize(code);
        if (Catalog.TryGetValue(normalized, out var info))
            return info;

        return new CurrencyInfo(normalized, normalized, ExponentOf(normalized), normalized);
    }

    /// <summary>Uppercase, trimmed ISO code. Blank input yields <see cref="Default"/>.</summary>
    public static string Normalize(string? code) =>
        string.IsNullOrWhiteSpace(code) ? Default : code.Trim().ToUpperInvariant();

    /// <summary>
    /// Decimal places for a currency. Unknown codes default to 2 — the common case — rather
    /// than throwing, because this sits on the payment path.
    /// </summary>
    public static int ExponentOf(string? code)
    {
        var normalized = Normalize(code);
        if (Catalog.TryGetValue(normalized, out var info)) return info.Exponent;
        if (ZeroDecimal.Contains(normalized)) return 0;
        if (ThreeDecimal.Contains(normalized)) return 3;
        return 2;
    }

    /// <summary>
    /// Convert a decimal amount into the integer minor units a payment processor expects.
    ///
    /// This replaces the hardcoded <c>* 100</c> that was previously applied to every currency.
    /// That was silently wrong for zero-decimal currencies: ¥5,000 was submitted as 500000,
    /// i.e. ¥500,000 — a 100x overcharge.
    ///
    /// Three-decimal currencies are additionally rounded to the nearest 10, which Stripe
    /// requires; submitting an amount not divisible by 10 is rejected outright.
    /// </summary>
    public static long ToMinorUnits(decimal amount, string? code)
    {
        var exponent = ExponentOf(code);
        var scaled = amount * Pow10(exponent);
        var minor = (long)Math.Round(scaled, MidpointRounding.AwayFromZero);

        if (exponent == 3)
            minor = (long)Math.Round(minor / 10m, MidpointRounding.AwayFromZero) * 10;

        return minor;
    }

    /// <summary>
    /// Convert integer minor units back into a decimal amount. Inverse of
    /// <see cref="ToMinorUnits"/>; replaces the hardcoded <c>/ 100</c> applied when reading
    /// amounts back from Stripe webhooks and invoices.
    /// </summary>
    public static decimal FromMinorUnits(long minorUnits, string? code) =>
        minorUnits / Pow10(ExponentOf(code));

    /// <summary>
    /// Format an amount for display, e.g. <c>¥5,000</c> or <c>$39.00</c>.
    ///
    /// Deliberately does NOT use <see cref="System.Globalization.CultureInfo"/>. Constructing a
    /// culture throws <c>CultureNotFoundException</c> when the app runs with
    /// <c>InvariantGlobalization=true</c> — the default in several slim container images — which
    /// would turn a formatting call on the billing path into a 500. Symbol plus invariant number
    /// formatting is predictable everywhere and cannot throw.
    ///
    /// Decimal places follow the currency's exponent, so zero-decimal currencies render without
    /// a misleading ".00".
    /// </summary>
    public static string Format(decimal amount, string? code)
    {
        var info = Get(code);
        var text = amount.ToString(
            "N" + info.Exponent.ToString(System.Globalization.CultureInfo.InvariantCulture),
            System.Globalization.CultureInfo.InvariantCulture);
        return info.Symbol + text;
    }

    private static decimal Pow10(int exponent) => exponent switch
    {
        0 => 1m,
        1 => 10m,
        2 => 100m,
        3 => 1000m,
        _ => (decimal)Math.Pow(10, exponent)
    };
}
