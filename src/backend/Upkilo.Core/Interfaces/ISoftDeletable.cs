namespace Upkilo.Core.Interfaces;

/// <summary>
/// Interface for entities that should be soft-deleted instead of hard-deleted from the database.
/// Helps prevent accidental data loss for critical business records.
/// </summary>
public interface ISoftDeletable
{
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
