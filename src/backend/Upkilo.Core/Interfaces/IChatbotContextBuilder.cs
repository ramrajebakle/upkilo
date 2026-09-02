namespace Upkilo.Core.Interfaces;

/// <summary>
/// Who is on the other end of the conversation. This decides which knowledge the assistant is
/// allowed to see, so it is a security boundary, not a formatting hint.
/// </summary>
public enum ChatAudience
{
    /// <summary>
    /// A member of the public on a business's booking widget. They may see that business's
    /// published information and nothing else - in particular, nothing about Upkilo.
    /// </summary>
    PublicVisitor,

    /// <summary>
    /// An authenticated user inside the tenant's own dashboard. They may additionally see
    /// Upkilo platform information, because they are the customer of the platform.
    /// </summary>
    TenantStaff
}

/// <summary>
/// The assembled, source-separated knowledge the assistant may use for one turn.
///
/// The three blocks are kept apart rather than concatenated by the caller so the prompt can
/// state where each fact came from and how to rank them. Mixing them is the failure mode this
/// type exists to prevent: a business's opening hours and Upkilo's billing policy are both
/// "facts", but answering one with the other is wrong in both directions.
/// </summary>
public sealed class ChatbotContext
{
    /// <summary>
    /// Facts about THIS tenant's business, read live from its own rows. Authoritative for any
    /// question about the business. Empty string when the tenant has published nothing.
    /// </summary>
    public string TenantFacts { get; init; } = string.Empty;

    /// <summary>
    /// The tenant's curated Q&amp;A entries. Authoritative, and outranks TenantFacts on overlap
    /// because a human deliberately wrote it.
    /// </summary>
    public string KnowledgeBase { get; init; } = string.Empty;

    /// <summary>
    /// Facts about the Upkilo platform itself. Populated only for <see cref="ChatAudience.TenantStaff"/>;
    /// always empty for a public visitor.
    /// </summary>
    public string PlatformFacts { get; init; } = string.Empty;

    /// <summary>
    /// True when no tenant-specific knowledge could be assembled at all. The prompt uses this to
    /// forbid answering business questions from the model's own priors, which is where invented
    /// prices and opening hours come from.
    /// </summary>
    public bool HasTenantKnowledge =>
        !string.IsNullOrWhiteSpace(TenantFacts) || !string.IsNullOrWhiteSpace(KnowledgeBase);
}

public interface IChatbotContextBuilder
{
    /// <summary>
    /// Assembles the knowledge available for one tenant and audience. Every query inside is
    /// filtered on the supplied tenantId explicitly, never on ambient state.
    /// </summary>
    Task<ChatbotContext> BuildAsync(Guid tenantId, ChatAudience audience, CancellationToken ct = default);
}
