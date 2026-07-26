namespace Upkilo.Core.Interfaces;

/// <summary>
/// Cloud Service Abstraction Layer — abstracts cloud provider operations
/// to allow switching between Azure, AWS, GCP without code changes.
/// </summary>
public interface ICloudStorageProvider
{
    Task<string> UploadAsync(string containerName, string blobName, Stream content, string contentType, CancellationToken ct = default);
    Task<Stream> DownloadAsync(string containerName, string blobName, CancellationToken ct = default);
    Task DeleteAsync(string containerName, string blobName, CancellationToken ct = default);
    Task<bool> ExistsAsync(string containerName, string blobName, CancellationToken ct = default);
    Task<string> GetSignedUrlAsync(string containerName, string blobName, TimeSpan expiry, CancellationToken ct = default);
    Task<IEnumerable<CloudBlobInfo>> ListAsync(string containerName, string? prefix = null, CancellationToken ct = default);
}

public interface ICloudSecretsProvider
{
    Task<string> GetSecretAsync(string secretName, CancellationToken ct = default);
    Task SetSecretAsync(string secretName, string value, CancellationToken ct = default);
    Task DeleteSecretAsync(string secretName, CancellationToken ct = default);
}

public interface ICloudQueueProvider
{
    Task SendMessageAsync(string queueName, string message, TimeSpan? delay = null, CancellationToken ct = default);
    Task<CloudQueueMessage?> ReceiveMessageAsync(string queueName, CancellationToken ct = default);
    Task DeleteMessageAsync(string queueName, string messageId, string popReceipt, CancellationToken ct = default);
}

public record CloudBlobInfo(string Name, long Size, DateTimeOffset? LastModified, string? ContentType);

public record CloudQueueMessage(string Id, string Body, string PopReceipt, int DequeueCount);
