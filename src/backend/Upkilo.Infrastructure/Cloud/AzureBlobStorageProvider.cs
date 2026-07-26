using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Interfaces;

namespace Upkilo.Infrastructure.Cloud;

/// <summary>
/// Azure Blob Storage implementation of the cloud storage abstraction.
/// Easily replaceable with AWS S3 or GCP Cloud Storage.
/// </summary>
public class AzureBlobStorageProvider : ICloudStorageProvider
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<AzureBlobStorageProvider> _logger;

    public AzureBlobStorageProvider(IConfiguration configuration, ILogger<AzureBlobStorageProvider> logger)
    {
        var connectionString = configuration.GetConnectionString("AzureBlobStorage")
            ?? configuration["AzureBlobStorage:ConnectionString"]
            ?? "UseDevelopmentStorage=true";
        _blobServiceClient = new BlobServiceClient(connectionString);
        _logger = logger;
    }

    public async Task<string> UploadAsync(string containerName, string blobName, Stream content, string contentType, CancellationToken ct = default)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync(cancellationToken: ct);

        var blobClient = containerClient.GetBlobClient(blobName);
        await blobClient.UploadAsync(content, new BlobHttpHeaders { ContentType = contentType }, cancellationToken: ct);

        _logger.LogInformation("Uploaded blob {BlobName} to {Container}", blobName, containerName);
        return blobClient.Uri.ToString();
    }

    public async Task<Stream> DownloadAsync(string containerName, string blobName, CancellationToken ct = default)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);
        var response = await blobClient.DownloadStreamingAsync(cancellationToken: ct);
        return response.Value.Content;
    }

    public async Task DeleteAsync(string containerName, string blobName, CancellationToken ct = default)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);
        await blobClient.DeleteIfExistsAsync(cancellationToken: ct);
        _logger.LogInformation("Deleted blob {BlobName} from {Container}", blobName, containerName);
    }

    public async Task<bool> ExistsAsync(string containerName, string blobName, CancellationToken ct = default)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);
        var response = await blobClient.ExistsAsync(ct);
        return response.Value;
    }

    public async Task<string> GetSignedUrlAsync(string containerName, string blobName, TimeSpan expiry, CancellationToken ct = default)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);

        if (!blobClient.CanGenerateSasUri)
        {
            _logger.LogWarning("Cannot generate SAS URI for {BlobName} — account key not available", blobName);
            return blobClient.Uri.ToString();
        }

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = containerName,
            BlobName = blobName,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.Add(expiry)
        };
        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        return blobClient.GenerateSasUri(sasBuilder).ToString();
    }

    public async Task<IEnumerable<CloudBlobInfo>> ListAsync(string containerName, string? prefix = null, CancellationToken ct = default)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var results = new List<CloudBlobInfo>();

        await foreach (var blob in containerClient.GetBlobsAsync(traits: BlobTraits.None, states: BlobStates.None, prefix: prefix, cancellationToken: ct))
        {
            results.Add(new CloudBlobInfo(
                blob.Name,
                blob.Properties.ContentLength ?? 0,
                blob.Properties.LastModified,
                blob.Properties.ContentType));
        }

        return results;
    }
}
