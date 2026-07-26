using System;
using System.Collections.Generic;

namespace Upkilo.Core.Entities;

public enum CommunicationType
{
    Email,
    SMS,
    WhatsApp,
    Push,
    Call,
    InApp,
    System
}

public enum CommunicationDirection
{
    Inbound,
    Outbound
}

public enum CommunicationStatus
{
    Pending,
    Queued,
    Sent,
    Delivered,
    Received,
    Failed,
    Read
}

/// <summary>
/// Log of all communications with clients
/// </summary>
public class CommunicationLog : TenantEntity
{
    public Guid? ClientId { get; set; }
    public Client? Client { get; set; }

    public Guid? UserId { get; set; }
    public User? User { get; set; }

    public CommunicationType Type { get; set; }
    public CommunicationDirection Direction { get; set; }
    
    public string? Subject { get; set; }
    public string Body { get; set; } = string.Empty;
    
    public CommunicationStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    
    public string? ReferenceId { get; set; } // Internal reference
    public string? ExternalReference { get; set; } // External reference (e.g. Twilio SID)
    
    public Dictionary<string, string> Metadata { get; set; } = new();
    
    public DateTime? DeliveredAt { get; set; }
    public DateTime? ReadAt { get; set; }
}
