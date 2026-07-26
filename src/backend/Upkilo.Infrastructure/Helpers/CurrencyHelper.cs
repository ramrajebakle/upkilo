using System.Globalization;

namespace Upkilo.Infrastructure.Helpers;

public static class CurrencyHelper
{
    private static readonly Dictionary<string, string> CurrencyCultureMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "USD", "en-US" },
        { "EUR", "fr-FR" }, // Generic Euro
        { "GBP", "en-GB" },
        { "INR", "en-IN" },
        { "JPY", "ja-JP" },
        { "CAD", "en-CA" },
        { "AUD", "en-AU" },
        { "SGD", "en-SG" },
        { "AED", "ar-AE" },
        { "SAR", "ar-SA" }
    };

    public static string FormatCurrency(decimal amount, string currencyCode)
    {
        if (string.IsNullOrWhiteSpace(currencyCode))
            currencyCode = "USD";

        if (CurrencyCultureMap.TryGetValue(currencyCode, out var cultureCode))
        {
            var culture = new CultureInfo(cultureCode);
            return amount.ToString("C", culture);
        }

        // Fallback: Display code and amount
        return $"{currencyCode} {amount:N2}";
    }

    public static string GetCurrencySymbol(string currencyCode)
    {
        if (CurrencyCultureMap.TryGetValue(currencyCode, out var cultureCode))
        {
            var culture = new CultureInfo(cultureCode);
            return culture.NumberFormat.CurrencySymbol;
        }

        return currencyCode;
    }
}
