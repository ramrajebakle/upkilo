using System;
using System.Collections.Generic;

namespace Upkilo.Core.Entities;

/// <summary>
/// Detailed notes for a client with history
/// </summary>
public class ClientNote : TenantEntity
{
    public Guid ClientId { get; set; }
    public Guid AuthorId { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsPrivate { get; set; } = true;
    public string? Category { get; set; } // e.g., "Medical", "Preference", "General"

    // Navigation
    public virtual Client? Client { get; set; }
    public virtual User? Author { get; set; }
}

