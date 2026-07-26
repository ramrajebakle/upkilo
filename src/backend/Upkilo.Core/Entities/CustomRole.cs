namespace Upkilo.Core.Entities;

/// <summary>
/// Custom role entity - tenant-defined roles with granular permissions
/// </summary>
public class CustomRole : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Dictionary<string, bool> Permissions { get; set; } = new();
    public bool IsSystem { get; set; } // Can't delete system roles
    public bool IsActive { get; set; } = true;

    // Navigation
    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
