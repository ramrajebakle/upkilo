using System;

namespace Upkilo.Core.Entities
{
    /// <summary>
    /// Represents a task or to-do in the system (e.g., follow-up with a lead).
    /// </summary>
    public class CrmTask : TenantEntity
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime? DueDate { get; set; }
        public Guid? AssignedTo { get; set; } // StaffId
        public string Priority { get; set; } = "Medium"; // Low, Medium, High
        public string Status { get; set; } = "Pending"; // Pending, InProgress, Completed, Deferred
        public Guid? RelatedId { get; set; } // Can be a ClientId, BookingId, etc.
        public string? RelatedType { get; set; } // "Client", "Booking", etc.

        // Navigation
        public virtual StaffMember? Assignee { get; set; }
    }
}
