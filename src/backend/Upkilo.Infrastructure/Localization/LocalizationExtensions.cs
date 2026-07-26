using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;

namespace Upkilo.Infrastructure.Localization;

/// <summary>
/// Configures localization/internationalization for the Upkilo platform.
/// Supports 10+ languages with fallback to English.
/// </summary>
public static class LocalizationExtensions
{
    private static readonly string[] SupportedCultures =
    {
        "en-US", "en-GB", "es-ES", "fr-FR", "de-DE",
        "pt-BR", "ja-JP", "zh-CN", "ko-KR", "ar-SA",
        "hi-IN", "it-IT", "nl-NL", "ru-RU", "tr-TR"
    };

    public static IServiceCollection AddUpkiloLocalization(this IServiceCollection services)
    {
        services.AddLocalization(options => options.ResourcesPath = "Resources");

        services.Configure<RequestLocalizationOptions>(options =>
        {
            var cultures = SupportedCultures
                .Select(c => new CultureInfo(c))
                .ToList();

            options.DefaultRequestCulture = new RequestCulture("en-US");
            options.SupportedCultures = cultures;
            options.SupportedUICultures = cultures;

            // Priority order: query string, cookie, Accept-Language header
            options.RequestCultureProviders = new List<IRequestCultureProvider>
            {
                new QueryStringRequestCultureProvider(),
                new CookieRequestCultureProvider(),
                new AcceptLanguageHeaderRequestCultureProvider()
            };
        });

        return services;
    }

    public static IApplicationBuilder UseUpkiloLocalization(this IApplicationBuilder app)
    {
        app.UseRequestLocalization();
        return app;
    }
}
