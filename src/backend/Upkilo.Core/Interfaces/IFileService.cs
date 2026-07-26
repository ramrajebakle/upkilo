using Upkilo.Core.Entities;

namespace Upkilo.Core.Interfaces;

/// <summary>
/// File service interface for Azure Blob Storage operations
/// </summary>
public interface IFileService
{
    /// <summary>
    /// Upload a file and return the URL
    /// </summary>
    Task<FileUploadResult> UploadAsync(Stream fileStream, string fileName, string contentType, Guid tenantId, FileCategory category);

    /// <summary>
    /// Delete a file by its URL or blob name
    /// </summary>
    Task<bool> DeleteAsync(string fileUrl);

    /// <summary>
    /// Get a temporary signed URL for private file access
    /// </summary>
    Task<string> GetSignedUrlAsync(string fileUrl, TimeSpan expiry);

    /// <summary>
    /// List files for a tenant in a category
    /// </summary>
    Task<IEnumerable<FileMetadata>> ListFilesAsync(Guid tenantId, FileCategory? category = null, int limit = 100);

    /// <summary>
    /// Get file metadata
    /// </summary>
    Task<FileMetadata?> GetMetadataAsync(string fileUrl);

    /// <summary>
    /// Save a byte array as a file and return the URL.
    /// </summary>
    Task<string> SaveFileAsync(byte[] data, string fileName, string contentType, Guid tenantId, FileCategory category = FileCategory.Exports);
}

public class FileUploadResult
{
    public string Url { get; set; } = string.Empty;
    public string BlobName { get; set; } = string.Empty;
    public long Size { get; set; }
    public string ContentType { get; set; } = string.Empty;
}

public class FileMetadata
{
    public string Url { get; set; } = string.Empty;
    public string BlobName { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long Size { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public FileCategory Category { get; set; }
}

public enum FileCategory
{
    ProfileImages,
    ServiceImages,
    Documents,
    Invoices,
    Exports,
    Imports,
    Other
}
