namespace Upkilo.Core.Interfaces;

/// <summary>
/// Cloud service abstraction layer for storage, messaging, and secrets
/// </summary>
public interface ICloudStorageService
{
    Task<string> UploadAsync(string containerName, string blobName, Stream content, string contentType);
    Task<Stream?> DownloadAsync(string containerName, string blobName);
    Task<bool> DeleteAsync(string containerName, string blobName);
    Task<string> GetSignedUrlAsync(string containerName, string blobName, TimeSpan expiry);
}

public interface ICloudMessageBus
{
    Task PublishAsync<T>(string topic, T message) where T : class;
    Task SubscribeAsync<T>(string topic, Func<T, Task> handler) where T : class;
}

public interface IDistributedCacheService
{
    Task<T?> GetAsync<T>(string key) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class;
    Task RemoveAsync(string key);
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null) where T : class;
    
    /// <summary>
    /// Acquires a distributed lock to prevent cache stampede
    /// </summary>
    Task<IAsyncDisposable?> AcquireLockAsync(string key, TimeSpan lockDuration);
}
