using System.Collections.Generic;
using Upkilo.Core.Events;

namespace Upkilo.Core.Entities;

/// <summary>
/// Base entity class with common audit fields and soft delete support
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Concurrency tracking
    public byte[]? RowVersion { get; set; }
    public int Version { get; set; } = 1;

    // Soft delete support
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    private readonly List<DomainEvent> _domainEvents = new();
    
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void AddDomainEvent(DomainEvent domainEvent) => _domainEvents.Add(domainEvent);
    public void RemoveDomainEvent(DomainEvent domainEvent) => _domainEvents.Remove(domainEvent);
    public void ClearDomainEvents() => _domainEvents.Clear();
}

/// <summary>
/// Base entity with tenant isolation
/// </summary>
public abstract class TenantEntity : BaseEntity
{
    public Guid TenantId { get; set; }
}
