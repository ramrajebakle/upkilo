using System;

namespace Upkilo.Core.Entities;

/// <summary>
/// Stores a user's saved search filter preferences.
/// </summary>
public class SavedSearchFilter : TenantEntity
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SearchType { get; set; } = "Global"; // Global, Clients, Bookings
    public string TargetEntity { get => SearchType; set => SearchType = value; } // Alias
    
    public string QueryString { get; set; } = string.Empty;
    public string FiltersJson { get; set; } = "{}"; // JSON map of advanced filters
    public string FilterJson { get => FiltersJson; set => FiltersJson = value; } // Alias
    public bool IsDefault { get; set; }
    public DateTime LastUsedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Logs a user's recent search queries for quick access.
/// </summary>
public class RecentSearch : TenantEntity
{
    public Guid UserId { get; set; }
    public string QueryString { get; set; } = string.Empty;
    public string SearchType { get; set; } = "Global"; // Global, Clients, Bookings
    public int ResultCount { get; set; }
    public DateTime SearchedAt { get; set; } = DateTime.UtcNow;
}
