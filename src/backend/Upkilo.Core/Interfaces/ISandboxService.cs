using System;
using System.Threading.Tasks;
using Upkilo.Core.Entities;

namespace Upkilo.Core.Interfaces;

/// <summary>
/// Service to provision and manage API Sandbox environments for developers.
/// </summary>
public interface ISandboxService
{
    /// <summary>
    /// Creates a new isolated sandbox environment for a developer.
    /// </summary>
    Task<SandboxEnvironment> CreateSandboxAsync(Guid userId, string? seedConfig = null);

    /// <summary>
    /// Resets an existing sandbox environment to its initial state.
    /// </summary>
    Task<SandboxEnvironment> ResetSandboxAsync(string sandboxId);

    /// <summary>
    /// Deletes a sandbox environment.
    /// </summary>
    Task DeleteSandboxAsync(string sandboxId);

    /// <summary>
    /// Checks if a sandbox is currently active and within its expiry date.
    /// </summary>
    Task<bool> IsSandboxValidAsync(string sandboxId);

    /// <summary>
    /// Records recent access in the sandbox for analytics and TTL tracking.
    /// </summary>
    Task RecordAccessAsync(string sandboxId);
}
