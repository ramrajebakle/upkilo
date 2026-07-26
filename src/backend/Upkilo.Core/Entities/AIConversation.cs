using System;
using System.Collections.Generic;

namespace Upkilo.Core.Entities;

public class AIConversation : TenantEntity
{
    public string ExternalId { get; set; } = string.Empty; // Channel-specific ID (e.g., phone number or IG handle)
    public ConversationChannel Channel { get; set; }
    public Guid? ClientId { get; set; }
    public ConversationStatus Status { get; set; } = ConversationStatus.Active;
    public string? LastMessage { get; set; }
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
    public string? Summary { get; set; }
    public string? Intent { get; set; }
    public bool HumanInteractionRequired { get; set; }
    public Guid? AssignedStaffId { get; set; }

    // Navigation
    public virtual Client? Client { get; set; }
    public virtual StaffMember? AssignedStaff { get; set; }
    public virtual ICollection<AIMessage> Messages { get; set; } = new List<AIMessage>();
}

public enum ConversationChannel
{
    SMS,
    WhatsApp,
    Instagram,
    Facebook,
    WebChat,
    Email
}

public enum ConversationStatus
{
    Active,
    HandoffRequested,
    HumanHandled,
    Closed
}
