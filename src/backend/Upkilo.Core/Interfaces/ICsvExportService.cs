namespace Upkilo.Core.Interfaces;

public interface ICsvExportService
{
    /// <summary>
    /// Generates a CSV byte array from an IEnumerable of objects.
    /// Uses reflection to write public properties as columns.
    /// </summary>
    byte[] ExportToCsv<T>(IEnumerable<T> data);

    /// <summary>
    /// Async version — delegates to ExportToCsv internally.
    /// </summary>
    Task<byte[]> ExportToCsvAsync<T>(IEnumerable<T> data) => Task.FromResult(ExportToCsv(data));
}
