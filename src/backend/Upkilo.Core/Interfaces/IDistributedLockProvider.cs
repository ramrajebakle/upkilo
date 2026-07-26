using System;
using System.Threading.Tasks;

namespace Upkilo.Core.Interfaces;

public interface IDistributedLockProvider
{
    /// <summary>
    /// Acquires a distributed lock.
    /// </summary>
    /// <param name="resource">The resource key to lock.</param>
    /// <param name="expiration">How long to hold the lock.</param>
    /// <param name="wait">How long to wait for the lock if held by someone else.</param>
    /// <param name="retry">Retry interval.</param>
    /// <returns>A disposable lock object or null if failed.</returns>
    Task<IDisposable?> AcquireLockAsync(string resource, TimeSpan expiration, TimeSpan? wait = null, TimeSpan? retry = null);
}
