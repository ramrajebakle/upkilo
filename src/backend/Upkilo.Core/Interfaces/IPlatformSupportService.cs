namespace Upkilo.Core.Interfaces;

/// <summary>
/// Identity Upkilo's own platform-support assistant runs under.
///
/// The public support bot on the marketing site has no tenant - the visitor is not a customer
/// yet. But <see cref="IAIService.GenerateTextAsync"/> meters every call against a tenant id, so
/// the spend has to land somewhere. It lands here: a well-known id that belongs to Upkilo rather
/// than to any customer.
///
/// Two properties follow from that, and both are deliberate:
///   - Abuse of the public bot burns Upkilo's own budget, never a customer's.
///   - The spend is still capped and still visible, because it is logged to AIUsageLogs like any
///     other usage and gated by a configured monthly budget (see AiService.CheckQuotaAsync).
///
/// There is intentionally no Tenants row for this id. AIUsageLog.TenantId carries no foreign key,
/// so the usage rows insert cleanly, and the absence of a row is what keeps the platform identity
/// out of every tenant-scoped query, listing and export in the product.
/// </summary>
public static class UpkiloPlatform
{
    /// <summary>
    /// Fixed, non-random id. It must stay constant across deployments or the monthly spend total
    /// resets and the budget cap stops meaning anything.
    /// </summary>
    public static readonly Guid TenantId = new("00000000-0000-0000-0000-00000000117a");

    /// <summary>Feature label written to AIUsageLogs, so platform spend is separable in reporting.</summary>
    public const string UsageFeature = "platform-support";
}

/// <summary>
/// The anonymous "what is Upkilo?" assistant behind the marketing-site widget.
///
/// This is a different assistant from <see cref="IChatbotService"/>, not a mode of it, because the
/// two have different data in scope and mixing them is the failure this split prevents. A tenant's
/// assistant may read that tenant's business rows; this one may read nothing but Upkilo's own
/// published plan catalogue. Since it is never handed a tenant id, there is no query it could run
/// that returns customer data - the isolation is structural rather than a rule the prompt asks the
/// model to follow.
/// </summary>
public interface IPlatformSupportService
{
    Task<PlatformSupportReply> AskAsync(string message, string history, CancellationToken ct = default);
}

public sealed class PlatformSupportReply
{
    public string Reply { get; init; } = string.Empty;

    /// <summary>
    /// True when the turn produced no usable answer - AI unavailable, quota exhausted, or input
    /// rejected. The caller uses it to decide whether to point the visitor at a human, and it is
    /// kept separate from <see cref="Reply"/> so a refusal is never mistaken for an answer.
    /// </summary>
    public bool IsFallback { get; init; }

    /// <summary>Set when the message was rejected outright as an injection attempt.</summary>
    public bool Rejected { get; init; }
}
