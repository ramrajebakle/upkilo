using System.Globalization;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Upkilo.API.Middleware;

/// <summary>
/// Middleware to standardize language and locale settings based on the Accept-Language header or tenant preferences.
/// Ensures consistent date, currency, and number formatting across the platform.
/// </summary>
public class LanguageStandardizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<LanguageStandardizationMiddleware> _logger;

    public LanguageStandardizationMiddleware(RequestDelegate next, ILogger<LanguageStandardizationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var userLanguage = context.Request.Headers["Accept-Language"].ToString().Split(',').FirstOrDefault();
        
        // Default to English (US) if nothing provided or invalid
        var cultureName = "en-US";

        if (!string.IsNullOrEmpty(userLanguage))
        {
            try
            {
                // Validate if it's a known culture
                var culture = CultureInfo.GetCultureInfo(userLanguage);
                cultureName = culture.Name;
            }
            catch (CultureNotFoundException)
            {
                _logger.LogWarning("Unsupported culture requested: {Culture}. Falling back to en-US.", userLanguage);
            }
        }

        // Apply culture to the current thread
        var finalCulture = new CultureInfo(cultureName);
        CultureInfo.CurrentCulture = finalCulture;
        CultureInfo.CurrentUICulture = finalCulture;

        // Add to response header for client-side synchronization
        context.Response.Headers["Content-Language"] = cultureName;

        await _next(context);
    }
}
