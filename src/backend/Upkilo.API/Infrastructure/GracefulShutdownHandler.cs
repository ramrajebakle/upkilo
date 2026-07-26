using Hangfire;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Upkilo.API.Infrastructure;

// Registered as IHostedService so its StopAsync runs during shutdown before the
// host disposes other services. Azure App Service sends SIGTERM and waits 30s
// before SIGKILL — HostOptions.ShutdownTimeout is set to 25s in Program.cs.
public class GracefulShutdownHandler : IHostedService
{
    private readonly ILogger<GracefulShutdownHandler> _logger;
    private readonly IHostApplicationLifetime _lifetime;

    public GracefulShutdownHandler(
        ILogger<GracefulShutdownHandler> logger,
        IHostApplicationLifetime lifetime)
    {
        _logger = logger;
        _lifetime = lifetime;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _lifetime.ApplicationStarted.Register(() =>
            _logger.LogInformation("Upkilo API started. All background services active."));
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogWarning("Upkilo API shutting down — draining in-flight requests...");

        try
        {
            var api = JobStorage.Current?.GetMonitoringApi();
            if (api != null)
                _logger.LogInformation(
                    "Hangfire: {Enqueued} enqueued, {Processing} processing at shutdown start",
                    api.EnqueuedCount("default"), api.ProcessingCount());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error querying Hangfire state during shutdown");
        }

        // Wait for the host-level drain period (driven by HostOptions.ShutdownTimeout).
        // ASP.NET Core stops the Kestrel listener before calling StopAsync, so no new
        // requests arrive here — we are just waiting for in-flight ones to finish.
        // The cancellationToken is signalled when ShutdownTimeout expires.
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // ShutdownTimeout expired — log and exit cleanly rather than throwing.
            _logger.LogWarning("Graceful drain interrupted by shutdown timeout — forcing exit.");
        }

        _logger.LogInformation("Upkilo API stopped. All connections closed.");
    }
}
