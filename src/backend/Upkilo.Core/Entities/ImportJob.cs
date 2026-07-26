namespace Upkilo.Core.Entities;

public class ImportJob
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public string EntityType { get; set; } = "clients"; // clients, bookings, etc.
    public string FileName { get; set; } = string.Empty;
    public string Status { get; set; } = "pending"; // pending, processing, completed, failed
    public int TotalRows { get; set; }
    public int ProcessedRows { get; set; }
    public int SuccessfulRows { get; set; }
    public int FailedRows { get; set; }
    public string? ErrorDetails { get; set; } // JSON array of errors
    public string? ColumnMapping { get; set; } // JSON dictionary of mapping
    public int? ProcessingTimeMs { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}
