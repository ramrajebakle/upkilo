using Upkilo.Core.Entities;

namespace Upkilo.Core.Interfaces;

public interface IPushNotificationService
{
    /// <summary>
    /// Sends a browser push notification to a specific user.
    /// </summary>
    Task SendBrowserPushAsync(Guid userId, string title, string message, string? actionUrl = null);

    /// <summary>
    /// Sends a mobile push notification to a specific user (FCM/APNS).
    /// </summary>
    Task SendMobilePushAsync(Guid userId, string title, string message, string? actionUrl = null);

    /// <summary>
    /// Sends a push notification to all active devices of a user.
    /// </summary>
    Task SendPushToUserAsync(Guid userId, string title, string message, string? actionUrl = null);

    /// <summary>
    /// Registers a new browser push subscription.
    /// </summary>
    Task RegisterBrowserSubscriptionAsync(Guid userId, WebPushSubscription subscription);

    /// <summary>
    /// Registers a new mobile device token.
    /// </summary>
    Task RegisterMobileTokenAsync(Guid userId, PushNotificationToken token);

    /// <summary>
    /// Unregisters or deactivates a subscription/token.
    /// </summary>
    Task UnregisterDeviceAsync(Guid userId, string identifier);
}
