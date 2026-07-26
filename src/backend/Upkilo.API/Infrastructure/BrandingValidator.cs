namespace Upkilo.API.Infrastructure;

/// <summary>
/// Shared validation/sanitization for tenant-supplied branding inputs.
/// WL-01: CSS injection prevention. WL-02: URL scheme enforcement.
/// </summary>
public static class BrandingValidator
{
    private static readonly string[] BlockedCssPatterns =
    [
        "url(",
        "@import",
        "expression(",
        "behavior:",
        "javascript:",
        "-moz-binding"
    ];

    /// <summary>
    /// WL-01: Rejects CSS containing external resource loads or dynamic expressions.
    /// Throws <see cref="ArgumentException"/> on violation so the controller can return 400.
    /// </summary>
    public static string? SanitizeCss(string? css)
    {
        if (string.IsNullOrWhiteSpace(css)) return null;
        if (css.Length > 50_000)
            throw new ArgumentException("Custom CSS exceeds the 50 KB limit.");
        foreach (var pattern in BlockedCssPatterns)
        {
            if (css.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException($"Custom CSS contains a disallowed pattern: '{pattern}'.");
        }
        return css;
    }

    /// <summary>
    /// WL-02: Enforces HTTPS-only URLs for logo / favicon to prevent tracking via HTTP beacons
    /// and block javascript: / data: scheme injection.
    /// </summary>
    public static string? ValidateHttpsUrl(string? url, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        if (url.Length > 2048)
            throw new ArgumentException($"{fieldName} URL exceeds the 2048-character limit.");
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            throw new ArgumentException($"{fieldName} must be a valid absolute URL.");
        if (uri.Scheme != Uri.UriSchemeHttps)
            throw new ArgumentException($"{fieldName} must use HTTPS.");
        return url;
    }
}
