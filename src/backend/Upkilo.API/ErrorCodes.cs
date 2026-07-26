namespace Upkilo.API;

/// <summary>
/// Standardized API error codes catalog.
/// Every error response includes an error code from this catalog
/// so clients can programmatically handle specific errors.
/// Format: DOMAIN_SPECIFIC_ERROR (e.g., BOOKING_SLOT_UNAVAILABLE)
/// </summary>
public static class ErrorCodes
{
    // ── Authentication ──────────────────────────────
    public const string AUTH_INVALID_CREDENTIALS = "AUTH_INVALID_CREDENTIALS";
    public const string AUTH_ACCOUNT_LOCKED = "AUTH_ACCOUNT_LOCKED";
    public const string AUTH_ACCOUNT_DISABLED = "AUTH_ACCOUNT_DISABLED";
    public const string AUTH_TOKEN_EXPIRED = "AUTH_TOKEN_EXPIRED";
    public const string AUTH_TOKEN_INVALID = "AUTH_TOKEN_INVALID";
    public const string AUTH_2FA_REQUIRED = "AUTH_2FA_REQUIRED";
    public const string AUTH_2FA_INVALID = "AUTH_2FA_INVALID";
    public const string AUTH_SESSION_EXPIRED = "AUTH_SESSION_EXPIRED";
    public const string AUTH_INSUFFICIENT_PERMISSIONS = "AUTH_INSUFFICIENT_PERMISSIONS";

    // ── Tenant ──────────────────────────────────────
    public const string TENANT_NOT_FOUND = "TENANT_NOT_FOUND";
    public const string TENANT_SUSPENDED = "TENANT_SUSPENDED";
    public const string TENANT_SUBSCRIPTION_EXPIRED = "TENANT_SUBSCRIPTION_EXPIRED";
    public const string TENANT_LIMIT_EXCEEDED = "TENANT_LIMIT_EXCEEDED";

    // ── Booking ─────────────────────────────────────
    public const string BOOKING_NOT_FOUND = "BOOKING_NOT_FOUND";
    public const string BOOKING_SLOT_UNAVAILABLE = "BOOKING_SLOT_UNAVAILABLE";
    public const string BOOKING_CONFLICT = "BOOKING_CONFLICT";
    public const string BOOKING_ALREADY_CANCELLED = "BOOKING_ALREADY_CANCELLED";
    public const string BOOKING_PAST_DATE = "BOOKING_PAST_DATE";
    public const string BOOKING_CANCELLATION_NOT_ALLOWED = "BOOKING_CANCELLATION_NOT_ALLOWED";
    public const string BOOKING_HOLD_EXPIRED = "BOOKING_HOLD_EXPIRED";
    public const string BOOKING_MAX_CAPACITY = "BOOKING_MAX_CAPACITY";

    // ── Client ──────────────────────────────────────
    public const string CLIENT_NOT_FOUND = "CLIENT_NOT_FOUND";
    public const string CLIENT_DUPLICATE = "CLIENT_DUPLICATE";
    public const string CLIENT_EMAIL_EXISTS = "CLIENT_EMAIL_EXISTS";

    // ── Staff ───────────────────────────────────────
    public const string STAFF_NOT_FOUND = "STAFF_NOT_FOUND";
    public const string STAFF_NOT_AVAILABLE = "STAFF_NOT_AVAILABLE";
    public const string STAFF_ON_BREAK = "STAFF_ON_BREAK";

    // ── Service ─────────────────────────────────────
    public const string SERVICE_NOT_FOUND = "SERVICE_NOT_FOUND";
    public const string SERVICE_INACTIVE = "SERVICE_INACTIVE";

    // ── Payment ─────────────────────────────────────
    public const string PAYMENT_FAILED = "PAYMENT_FAILED";
    public const string PAYMENT_DECLINED = "PAYMENT_DECLINED";
    public const string PAYMENT_ALREADY_PROCESSED = "PAYMENT_ALREADY_PROCESSED";
    public const string PAYMENT_REFUND_EXCEEDS_AMOUNT = "PAYMENT_REFUND_EXCEEDS_AMOUNT";
    public const string PAYMENT_PROMO_INVALID = "PAYMENT_PROMO_INVALID";
    public const string PAYMENT_PROMO_EXPIRED = "PAYMENT_PROMO_EXPIRED";

    // ── Rate Limiting ───────────────────────────────
    public const string RATE_LIMIT_EXCEEDED = "RATE_LIMIT_EXCEEDED";
    public const string IDEMPOTENCY_DUPLICATE = "IDEMPOTENCY_DUPLICATE";

    // ── Validation ──────────────────────────────────
    public const string VALIDATION_REQUIRED_FIELD = "VALIDATION_REQUIRED_FIELD";
    public const string VALIDATION_INVALID_FORMAT = "VALIDATION_INVALID_FORMAT";
    public const string VALIDATION_OUT_OF_RANGE = "VALIDATION_OUT_OF_RANGE";

    // ── File Upload ─────────────────────────────────
    public const string FILE_TOO_LARGE = "FILE_TOO_LARGE";
    public const string FILE_INVALID_TYPE = "FILE_INVALID_TYPE";
    public const string FILE_UPLOAD_FAILED = "FILE_UPLOAD_FAILED";

    // ── AI & Automation ─────────────────────────────
    public const string AI_QUOTA_EXCEEDED = "AI_QUOTA_EXCEEDED";
    public const string AI_PROVIDER_ERROR = "AI_PROVIDER_ERROR";
    public const string AI_CONTENT_FILTERED = "AI_CONTENT_FILTERED";
    public const string AUTOMATION_WORKFLOW_INVALID = "AUTOMATION_WORKFLOW_INVALID";
    public const string AUTOMATION_EXECUTION_FAILED = "AUTOMATION_EXECUTION_FAILED";

    // ── Webhooks & Integrations ─────────────────────
    public const string WEBHOOK_DELIVERY_FAILED = "WEBHOOK_DELIVERY_FAILED";
    public const string INTEGRATION_UNAUTHORIZED = "INTEGRATION_UNAUTHORIZED";
    public const string INTEGRATION_API_ERROR = "INTEGRATION_API_ERROR";

    // ── General ─────────────────────────────────────
    public const string INTERNAL_ERROR = "INTERNAL_ERROR";
    public const string NOT_FOUND = "NOT_FOUND";
    public const string CONFLICT = "CONFLICT";
    public const string FORBIDDEN = "FORBIDDEN";
    public const string TIMEOUT = "TIMEOUT";
    public const string BAD_GATEWAY = "BAD_GATEWAY";
    public const string SERVICE_UNAVAILABLE = "SERVICE_UNAVAILABLE";
}
