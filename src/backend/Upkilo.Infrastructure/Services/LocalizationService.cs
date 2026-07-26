using Microsoft.Extensions.Localization;
using System.Globalization;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// Localization/i18n service providing string translations via resource files
/// </summary>
public class LocalizationService
{
    private readonly IStringLocalizer _localizer;
    private static readonly string[] SupportedLocales = { "en", "es", "fr", "de", "pt", "ar", "hi", "ja", "zh", "ko" };

    public LocalizationService(IStringLocalizerFactory factory)
    {
        _localizer = factory.Create("SharedResources", typeof(LocalizationService).Assembly.FullName!);
    }

    public string Translate(string key, string? locale = null)
    {
        if (!string.IsNullOrEmpty(locale))
        {
            CultureInfo.CurrentUICulture = new CultureInfo(locale);
        }
        return _localizer[key];
    }

    public string Translate(string key, string locale, params object[] args)
    {
        CultureInfo.CurrentUICulture = new CultureInfo(locale);
        return _localizer[key, args];
    }

    public IReadOnlyList<string> GetSupportedLocales() => SupportedLocales;

    public bool IsSupported(string locale)
    {
        return SupportedLocales.Contains(locale, StringComparer.OrdinalIgnoreCase);
    }

    public string DetectLocale(string? acceptLanguageHeader)
    {
        if (string.IsNullOrEmpty(acceptLanguageHeader)) return "en";

        // Parse Accept-Language header: "en-US,en;q=0.9,fr;q=0.8"
        var preferred = acceptLanguageHeader
            .Split(',')
            .Select(lang =>
            {
                var parts = lang.Trim().Split(';');
                var locale = parts[0].Trim().Split('-')[0]; // Normalize "en-US" to "en"
                var quality = parts.Length > 1 ? double.Parse(parts[1].Replace("q=", "")) : 1.0;
                return (locale, quality);
            })
            .OrderByDescending(x => x.quality)
            .FirstOrDefault();

        return IsSupported(preferred.locale) ? preferred.locale : "en";
    }
}
