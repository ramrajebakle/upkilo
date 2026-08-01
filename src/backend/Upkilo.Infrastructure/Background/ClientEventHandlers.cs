using MediatR;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Events;
using Upkilo.Core.Interfaces;

namespace Upkilo.Infrastructure.Background;

/// <summary>
/// Wrapper for MediatR to handle ClientCreated domain events.
/// </summary>
public class ClientCreatedNotification : INotification
{
    public ClientCreated Event { get; }

    public ClientCreatedNotification(ClientCreated evt) => Event = evt;
}

/// <summary>
/// Handles ClientCreated events by triggering the Auto-Onboarding workflow.
/// </summary>
public class ClientCreatedWorkflowHandler : INotificationHandler<ClientCreatedNotification>
{
    private readonly ITriggerDispatcher _triggerDispatcher;
    private readonly ILogger<ClientCreatedWorkflowHandler> _logger;

    public ClientCreatedWorkflowHandler(
        ITriggerDispatcher triggerDispatcher,
        ILogger<ClientCreatedWorkflowHandler> logger)
    {
        _triggerDispatcher = triggerDispatcher;
        _logger = logger;
    }

    public async Task Handle(ClientCreatedNotification notification, CancellationToken cancellationToken)
    {
        var evt = notification.Event;
        _logger.LogInformation("Handling ClientCreated event to trigger onboarding workflow for client {ClientId}", evt.ClientId);

        try
        {
            var data = new
            {
                ClientId = evt.ClientId,
                Email = evt.Email,
                FirstName = evt.FirstName,
                LastName = evt.LastName,
                Source = evt.Source
            };

            // Trigger "client.created" workflow which kicks off Auto-Onboarding
            await _triggerDispatcher.DispatchAsync("client.created", data, evt.TenantId);

            _logger.LogInformation("Successfully dispatched client.created trigger for {ClientId}", evt.ClientId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dispatching workflow trigger for ClientCreated: {ClientId}", evt.ClientId);
        }
    }
}
