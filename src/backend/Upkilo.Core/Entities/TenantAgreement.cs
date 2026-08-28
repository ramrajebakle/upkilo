namespace Upkilo.Core.Entities;

/// <summary>
/// A legal agreement between Upkilo and a tenant — currently the HIPAA Business
/// Associate Agreement and the uptime SLA.
///
/// Why this is not GdprConsent: that entity models a *client's* consent inside a
/// tenant (it carries a required ClientId and is scoped tenant -> client). The BAA
/// runs on the other axis, platform -> tenant, and the sign endpoint was passing
/// Guid.Empty as the ClientId to force it into that shape. It also had nowhere to
/// put the parts of a signature that matter: the endpoint validated
/// AuthorizedSignatoryName and AuthorizedSignatoryTitle, then wrote them to a log
/// line and discarded them — so the record could show that *someone* at an IP
/// accepted, but not who bound the entity or in what capacity, which is the whole
/// purpose of a signature block.
///
/// GdprConsent rows keep being written for HIPAA_BAA so the existing feature gate
/// in VerticalsController keeps working unchanged; this table is the evidence and
/// the thing platform admins manage.
/// </summary>
public class TenantAgreement : TenantEntity
{
    public AgreementType Type { get; set; }

    public AgreementStatus Status { get; set; } = AgreementStatus.NotSigned;

    /// <summary>Version of the document that was agreed, e.g. "2024.1".</summary>
    public string? DocumentVersion { get; set; }

    // ── Signature block ──────────────────────────────────────────────────────
    /// <summary>Who signed, as typed by them. Null until signed.</summary>
    public string? SignatoryName { get; set; }

    /// <summary>The capacity they signed in — the half that makes a signature binding.</summary>
    public string? SignatoryTitle { get; set; }

    public DateTime? SignedAt { get; set; }
    public string? SignedFromIp { get; set; }
    public string? UserAgent { get; set; }

    // ── Term ─────────────────────────────────────────────────────────────────
    public DateTime? EffectiveFrom { get; set; }

    /// <summary>
    /// Null means no fixed end. Set it when a document version is superseded, so
    /// the admin view can show who is still on an old BAA.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    // ── SLA-only ─────────────────────────────────────────────────────────────
    /// <summary>
    /// Contractual uptime target as a percentage, e.g. 99.9m. Null for a BAA, and
    /// null for tenants on no uptime commitment — which is every tenant until one
    /// is actually negotiated. Nothing measures against this yet; it records what
    /// was agreed so support and billing can see it.
    /// </summary>
    public decimal? UptimeTargetPercent { get; set; }

    /// <summary>Free-text for the platform team — contract reference, caveats, who negotiated it.</summary>
    public string? Notes { get; set; }

    public Tenant? Tenant { get; set; }
}

public enum AgreementType
{
    HipaaBaa,
    Sla
}

public enum AgreementStatus
{
    NotSigned,
    Signed,
    Expired,
    Terminated
}
