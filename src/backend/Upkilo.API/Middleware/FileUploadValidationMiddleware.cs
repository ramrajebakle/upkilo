using Microsoft.AspNetCore.Http;

namespace Upkilo.API.Middleware;

/// <summary>
/// File upload validation middleware.
/// Enforces file size limits and MIME type whitelisting
/// on all multipart file uploads.
///
/// Limits:
///   - Max 10MB per file (configurable)
///   - Max 50MB total per request
///   - Whitelist: images, PDFs, CSV, Excel, text
///   - Block: executables, scripts, archives
/// </summary>
public class FileUploadValidationMiddleware
{
    private readonly RequestDelegate _next;
    private const long MaxFileSizeBytes = 10 * 1024 * 1024;       // 10MB per file
    private const long MaxRequestSizeBytes = 50 * 1024 * 1024;    // 50MB total

    private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        // Images — SVG intentionally excluded: browsers execute inline scripts in SVG,
        // making served SVG files a stored-XSS vector (VULN-005 fix).
        "image/jpeg", "image/png", "image/gif", "image/webp",
        // Documents
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        // Data
        "text/csv", "text/plain", "application/json",
        // Video (for before/after galleries)
        "video/mp4", "video/webm"
    };

    private static readonly HashSet<string> BlockedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".exe", ".bat", ".cmd", ".sh", ".ps1", ".vbs", ".js", ".msi",
        ".dll", ".sys", ".com", ".scr", ".pif", ".jar", ".py", ".rb",
        ".php", ".asp", ".aspx", ".jsp", ".cgi", ".pl",
        // SVG blocked at extension level too — contains executable XML (VULN-005)
        ".svg", ".svgz"
    };

    public FileUploadValidationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.HasFormContentType && context.Request.Form.Files.Count > 0)
        {
            long totalSize = 0;

            foreach (var file in context.Request.Form.Files)
            {
                // Check individual file size
                if (file.Length > MaxFileSizeBytes)
                {
                    context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        error = "FILE_TOO_LARGE",
                        message = $"File '{file.FileName}' exceeds maximum size of 10MB",
                        maxSizeMB = MaxFileSizeBytes / (1024 * 1024)
                    });
                    return;
                }

                totalSize += file.Length;

                // Check total request size
                if (totalSize > MaxRequestSizeBytes)
                {
                    context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        error = "REQUEST_TOO_LARGE",
                        message = "Total upload size exceeds 50MB limit",
                        maxTotalSizeMB = MaxRequestSizeBytes / (1024 * 1024)
                    });
                    return;
                }

                // Check MIME type whitelist
                if (!AllowedMimeTypes.Contains(file.ContentType))
                {
                    context.Response.StatusCode = StatusCodes.Status415UnsupportedMediaType;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        error = "UNSUPPORTED_FILE_TYPE",
                        message = $"File type '{file.ContentType}' is not allowed",
                        allowedTypes = AllowedMimeTypes
                    });
                    return;
                }

                // Check extension blocklist
                var extension = Path.GetExtension(file.FileName);
                if (!string.IsNullOrEmpty(extension) && BlockedExtensions.Contains(extension))
                {
                    context.Response.StatusCode = StatusCodes.Status415UnsupportedMediaType;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        error = "BLOCKED_FILE_EXTENSION",
                        message = $"File extension '{extension}' is not allowed for security reasons"
                    });
                    return;
                }
            }
        }

        await _next(context);
    }
}
