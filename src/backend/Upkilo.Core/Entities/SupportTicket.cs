using System;

namespace Upkilo.Core.Entities;

public class SupportTicket : TenantEntity
{
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TicketStatus Status { get; set; } = TicketStatus.Open;
    public TicketPriority Priority { get; set; } = TicketPriority.Normal;

    public string? ContactEmail { get; set; } // For guest tickets
    public DateTime? SlaExpiresAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? ClosedAt { get; set; }

    public Guid SubmittedByUserId { get; set; }
    public virtual User? SubmittedByUser { get; set; }

    public Guid? AssignedToAdminId { get; set; }

    public virtual ICollection<SupportTicketComment> Comments { get; set; } = new List<SupportTicketComment>();
}

public class SupportTicketComment : TenantEntity
{
    public Guid TicketId { get; set; }
    public virtual SupportTicket? Ticket { get; set; }

    public string Content { get; set; } = string.Empty;
    public Guid AuthorUserId { get; set; }
    public bool IsInternal { get; set; } // Visible only to staff
}

public enum TicketStatus
{
    Open,
    InProgress,
    WaitingForCustomer,
    Resolved,
    Closed
}

public enum TicketPriority
{
    Low,
    Normal,
    High,
    Critical,
    Urgent
}
