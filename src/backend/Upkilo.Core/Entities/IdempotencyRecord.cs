namespace Upkilo.Core.Entities;

/// <summary>
/// Database entity for storing idempotency records.
/// Used by IdempotencyMiddleware to prevent duplicate processing
/// of the same request (e.g., double-bookings, double-charges).
/// Keys are automatically expired after 24 hours.
/// </summary>
public class IdempotencyRecord
{
    public int Id { get; set; }

    /// <summary>
    /// Composite key: "{TenantId}:{ClientKey}"
    /// </summary>
    public string Key { get; set; } = string.Empty;

    public int ResponseStatusCode { get; set; }
    public string? ResponseBody { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}
