using Upkilo.Core.Entities;

namespace Upkilo.Core.Interfaces;

/// <summary>
/// Interface for migrating data from external booking platforms (Calendly, Acuity, etc.)
/// </summary>
public interface IMigrationService
{
    /// <summary>
    /// Validates external credentials and fetches an overview of data to migrate
    /// </summary>
    Task<MigrationOverview> GetMigrationOverviewAsync(string provider, string apiKey, string? extraCredentials = null);

    /// <summary>
    /// Starts the migration process in the background
    /// </summary>
    Task<ImportJob> StartMigrationAsync(Guid tenantId, Guid userId, MigrationRequest request);
}

public class MigrationOverview
{
    public string Provider { get; set; } = string.Empty;
    public int ServiceCount { get; set; }
    public int StaffCount { get; set; }
    public int BookingCount { get; set; }
    public List<string> FoundServices { get; set; } = new();
    public List<string> FoundStaff { get; set; } = new();
}

public class MigrationRequest
{
    public string Provider { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string? ExtraCredentials { get; set; }
    public bool ImportServices { get; set; } = true;
    public bool ImportStaff { get; set; } = true;
    public bool ImportBookings { get; set; } = true;
}
