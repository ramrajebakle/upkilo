using Upkilo.Core.Entities;

namespace Upkilo.Core.Interfaces;

public interface IImportService
{
    Task<ImportAnalysis> AnalyzeImportAsync(Stream fileStream, string entityType);
    Task<ImportJob> StartImportAsync(Guid tenantId, Guid userId, string entityType, Stream fileStream, string fileName, Dictionary<string, string>? columnMapping = null);
    Task<ImportJob?> GetJobStatusAsync(Guid jobId);
    Task<IEnumerable<ImportJob>> GetJobHistoryAsync(Guid tenantId, int limit = 10);
    Task<byte[]> GetTemplateAsync(string entityType);
}

public class ImportAnalysis
{
    public List<string> Headers { get; set; } = new();
    public List<Dictionary<string, string>> PreviewRows { get; set; } = new();
    public int EstimatedRows { get; set; }
}
