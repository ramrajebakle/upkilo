using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Upkilo.Core.Entities;

namespace Upkilo.Core.Interfaces;

public interface IGdprService
{
    Task<GdprExportDto> ExportDataAsync(Guid tenantId, Guid clientId);
    Task<bool> DeleteDataAsync(Guid tenantId, Guid clientId, string reason);
    Task<bool> AnonymizeDataAsync(Guid tenantId, Guid clientId);
    Task<IEnumerable<DataProcessingLog>> GetProcessingLogsAsync(Guid tenantId, Guid clientId);
}

public class GdprExportDto
{
    public ClientDataExport? Client { get; set; }
    public List<BookingExport> Bookings { get; set; } = new();
    public List<MessageExport> Messages { get; set; } = new();
    public DateTime ExportedAt { get; set; } = DateTime.UtcNow;
}

public class ClientDataExport
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class BookingExport
{
    public DateTime Date { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class MessageExport
{
    public DateTime SentAt { get; set; }
    public string Channel { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
}
