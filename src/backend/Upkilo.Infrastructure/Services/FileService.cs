using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Interfaces;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// Azure Blob Storage file service implementation
/// </summary>
public class FileService : IFileService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<FileService> _logger;
    private readonly string _containerName;
    
    // Configuration
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB
    private static readonly string[] AllowedContentTypes = 
    {
        "image/jpeg", "image/png", "image/gif", "image/webp",
        "application/pdf", "text/csv",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" // xlsx
    };

    private readonly ISecretProvider _secretProvider;

    public FileService(IConfiguration configuration, ISecretProvider secretProvider, ILogger<FileService> logger)
    {
        _secretProvider = secretProvider;
        var connectionString = _secretProvider.GetSecret("Azure--Storage--ConnectionString") 
            ?? configuration["Azure:Storage:ConnectionString"]
            ?? throw new InvalidOperationException("Azure Storage ConnectionString not configured");
            
        _containerName = configuration["Azure:Storage:ContainerName"] ?? "upkilo-files";
        
        _blobServiceClient = new BlobServiceClient(connectionString);
        _logger = logger;
    }

    public async Task<FileUploadResult> UploadAsync(
        Stream fileStream, 
        string fileName, 
        string contentType, 
        Guid tenantId, 
        FileCategory category)
    {
        // Validate file size
        if (fileStream.Length > MaxFileSizeBytes)
            throw new InvalidOperationException($"File size exceeds maximum allowed ({MaxFileSizeBytes / 1024 / 1024} MB)");

        // Validate content type
        if (!AllowedContentTypes.Contains(contentType.ToLower()))
            throw new InvalidOperationException($"Content type '{contentType}' is not allowed");

        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

        // Generate blob name: tenants/{tenantId}/{category}/{guid}-{filename}
        var safeFileName = SanitizeFileName(fileName);
        var blobName = $"tenants/{tenantId}/{category.ToString().ToLower()}/{Guid.NewGuid()}-{safeFileName}";
        
        var blobClient = containerClient.GetBlobClient(blobName);

        var headers = new BlobHttpHeaders
        {
            ContentType = contentType,
            CacheControl = "public, max-age=31536000" // 1 year cache for static files
        };

        var metadata = new Dictionary<string, string>
        {
            { "tenantId", tenantId.ToString() },
            { "category", category.ToString() },
            { "originalFileName", fileName },
            { "uploadedAt", DateTime.UtcNow.ToString("O") }
        };

        var uploadPolicy = ResiliencePolicies.GetGenericRetryPolicy();
        await uploadPolicy.ExecuteAsync(async (ct) => 
        {
            await blobClient.UploadAsync(fileStream, new BlobUploadOptions
            {
                HttpHeaders = headers,
                Metadata = metadata
            });
        });

        _logger.LogInformation("File uploaded: {BlobName} for tenant {TenantId}", blobName, tenantId);

        return new FileUploadResult
        {
            Url = blobClient.Uri.ToString(),
            BlobName = blobName,
            Size = fileStream.Length,
            ContentType = contentType
        };
    }

    public async Task<bool> DeleteAsync(string fileUrl)
    {
        try
        {
            var blobName = ExtractBlobName(fileUrl);
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(blobName);
            
            var deletePolicy = ResiliencePolicies.GetGenericRetryPolicy();
            var response = await deletePolicy.ExecuteAsync(async (ct) => 
            {
                return await blobClient.DeleteIfExistsAsync();
            });
            _logger.LogInformation("File deleted: {BlobName}", blobName);
            return response.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file: {FileUrl}", fileUrl);
            return false;
        }
    }

    public Task<string> GetSignedUrlAsync(string fileUrl, TimeSpan expiry)
    {
        var blobName = ExtractBlobName(fileUrl);
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        var blobClient = containerClient.GetBlobClient(blobName);

        if (!blobClient.CanGenerateSasUri)
            throw new InvalidOperationException("Cannot generate SAS URI. Ensure connection string has account key.");

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = _containerName,
            BlobName = blobName,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.Add(expiry)
        };
        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        var sasUri = blobClient.GenerateSasUri(sasBuilder);
        return Task.FromResult(sasUri.ToString());
    }

    public async Task<IEnumerable<FileMetadata>> ListFilesAsync(Guid tenantId, FileCategory? category = null, int limit = 100)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        var prefix = category.HasValue 
            ? $"tenants/{tenantId}/{category.Value.ToString().ToLower()}/"
            : $"tenants/{tenantId}/";

        var results = new List<FileMetadata>();
        
        await foreach (var blob in containerClient.GetBlobsAsync(traits: BlobTraits.Metadata, states: BlobStates.None, prefix: prefix, cancellationToken: default))
        {
            if (results.Count >= limit) break;
            
            results.Add(new FileMetadata
            {
                Url = $"{containerClient.Uri}/{blob.Name}",
                BlobName = blob.Name,
                FileName = blob.Metadata.TryGetValue("originalFileName", out var name) ? name : Path.GetFileName(blob.Name),
                Size = blob.Properties.ContentLength ?? 0,
                ContentType = blob.Properties.ContentType ?? "application/octet-stream",
                UploadedAt = blob.Properties.CreatedOn?.UtcDateTime ?? DateTime.UtcNow,
                Category = ParseCategory(blob.Name)
            });
        }

        return results;
    }

    public async Task<FileMetadata?> GetMetadataAsync(string fileUrl)
    {
        try
        {
            var blobName = ExtractBlobName(fileUrl);
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            var props = await blobClient.GetPropertiesAsync();
            
            return new FileMetadata
            {
                Url = fileUrl,
                BlobName = blobName,
                FileName = props.Value.Metadata.TryGetValue("originalFileName", out var name) ? name : Path.GetFileName(blobName),
                Size = props.Value.ContentLength,
                ContentType = props.Value.ContentType,
                UploadedAt = props.Value.CreatedOn.UtcDateTime,
                Category = ParseCategory(blobName)
            };
        }
        catch
        {
            return null;
        }
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(fileName.Where(c => !invalid.Contains(c)).ToArray())
            .Replace(" ", "_")
            .ToLowerInvariant();
    }

    private string ExtractBlobName(string fileUrl)
    {
        // Handle both full URL and blob name
        if (fileUrl.Contains(_containerName))
        {
            var uri = new Uri(fileUrl);
            return uri.AbsolutePath.TrimStart('/').Replace($"{_containerName}/", "");
        }
        return fileUrl;
    }

    private static FileCategory ParseCategory(string blobName)
    {
        var parts = blobName.Split('/');
        if (parts.Length >= 3 && Enum.TryParse<FileCategory>(parts[2], true, out var category))
            return category;
        return FileCategory.Other;
    }

    public async Task<string> SaveFileAsync(byte[] data, string fileName, string contentType, Guid tenantId, FileCategory category = FileCategory.Exports)
    {
        using var stream = new MemoryStream(data);
        var result = await UploadAsync(stream, fileName, contentType, tenantId, category);
        return result.Url;
    }
}
