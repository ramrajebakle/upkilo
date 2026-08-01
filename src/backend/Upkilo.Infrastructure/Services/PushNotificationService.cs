using Microsoft.Azure.NotificationHubs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;
using WebPush;

namespace Upkilo.Infrastructure.Services;

public class PushNotificationService : IPushNotificationService
{
    private readonly AppDbContext _context;
    private readonly ILogger<PushNotificationService> _logger;
    private readonly IConfiguration _configuration;
    private readonly ISecretProvider _secretProvider;

    public PushNotificationService(
        AppDbContext context,
        ILogger<PushNotificationService> logger,
        IConfiguration configuration,
        ISecretProvider secretProvider)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
        _secretProvider = secretProvider;
    }

    public async Task SendBrowserPushAsync(Guid userId, string title, string message, string? actionUrl = null)
    {
        var subscriptions = await _context.WebPushSubscriptions
            .Where(s => s.UserId == userId && s.IsActive)
            .ToListAsync();

        if (!subscriptions.Any()) return;

        var publicKey = await _secretProvider.GetSecretAsync("Push:Vapid:PublicKey");
        var privateKey = await _secretProvider.GetSecretAsync("Push:Vapid:PrivateKey");
        var subject = _configuration["Push:Vapid:Subject"] ?? "mailto:admin@upkilo.com";

        if (string.IsNullOrEmpty(publicKey) || string.IsNullOrEmpty(privateKey))
        {
            _logger.LogWarning("VAPID keys are not configured. Browser push skipped.");
            return;
        }

        var vapidDetails = new VapidDetails(subject, publicKey, privateKey);
        var webPushClient = new WebPushClient();

        var payload = System.Text.Json.JsonSerializer.Serialize(new
        {
            title,
            message,
            actionUrl,
            timestamp = DateTime.UtcNow
        });

        foreach (var sub in subscriptions)
        {
            try
            {
                var pushSubscription = new PushSubscription(sub.Endpoint, sub.P256dh, sub.Auth);
                await webPushClient.SendNotificationAsync(pushSubscription, payload, vapidDetails);
            }
            catch (WebPushException ex)
            {
                _logger.LogError(ex, "Failed to send browser push to {Endpoint}. Status: {Status}", sub.Endpoint, ex.StatusCode);
                if (ex.StatusCode == System.Net.HttpStatusCode.Gone || ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    sub.IsActive = false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending browser push to {UserId}", userId);
            }
        }

        await _context.SaveChangesAsync();
    }

    public async Task SendMobilePushAsync(Guid userId, string title, string message, string? actionUrl = null)
    {
        var tokens = await _context.PushNotificationTokens
            .Where(t => t.UserId == userId && t.IsActive)
            .ToListAsync();

        if (!tokens.Any()) return;

        var connectionString = await _secretProvider.GetSecretAsync("Azure:NotificationHub:ConnectionString");
        var hubName = _configuration["Azure:NotificationHub:HubName"];

        if (string.IsNullOrEmpty(connectionString) || string.IsNullOrEmpty(hubName))
        {
            _logger.LogWarning("Azure Notification Hub is not configured. Mobile push skipped.");
            return;
        }

        var hub = NotificationHubClient.CreateClientFromConnectionString(connectionString, hubName);

        // Map userId to tags for targetting. 
        // In a real scenario, we'd use the Hub's SendNotificationAsync with a tag like "user:<userId>"
        // For simplicity, we can send to a specific tag.
        var userTag = $"user:{userId}";

        var androidPayload = $"{{\"data\":{{\"title\":\"{title}\",\"message\":\"{message}\",\"actionUrl\":\"{actionUrl}\"}}}}";
        var iosPayload = $"{{\"aps\":{{\"alert\":{{\"title\":\"{title}\",\"body\":\"{message}\"}},\"badge\":1}},\"actionUrl\":\"{actionUrl}\"}}";

        try
        {
            await hub.SendFcmNativeNotificationAsync(androidPayload, userTag);
            await hub.SendAppleNativeNotificationAsync(iosPayload, userTag);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending mobile push to user tag {Tag}", userTag);
        }
    }

    public async Task SendPushToUserAsync(Guid userId, string title, string message, string? actionUrl = null)
    {
        await Task.WhenAll(
            SendBrowserPushAsync(userId, title, message, actionUrl),
            SendMobilePushAsync(userId, title, message, actionUrl)
        );
    }

    public async Task RegisterBrowserSubscriptionAsync(Guid userId, WebPushSubscription subscription)
    {
        var existing = await _context.WebPushSubscriptions
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Endpoint == subscription.Endpoint);

        if (existing != null)
        {
            existing.P256dh = subscription.P256dh;
            existing.Auth = subscription.Auth;
            existing.IsActive = true;
            existing.RegisteredAt = DateTime.UtcNow;
        }
        else
        {
            subscription.UserId = userId;
            subscription.RegisteredAt = DateTime.UtcNow;
            _context.WebPushSubscriptions.Add(subscription);
        }

        await _context.SaveChangesAsync();
    }

    public async Task RegisterMobileTokenAsync(Guid userId, PushNotificationToken token)
    {
        var existing = await _context.PushNotificationTokens
            .FirstOrDefaultAsync(t => t.UserId == userId && t.DeviceToken == token.DeviceToken);

        if (existing != null)
        {
            existing.IsActive = true;
            existing.RegisteredAt = DateTime.UtcNow;
        }
        else
        {
            token.UserId = userId;
            token.RegisteredAt = DateTime.UtcNow;
            _context.PushNotificationTokens.Add(token);

            // Register with Azure Notification Hub
            await RegisterWithNotificationHubAsync(userId, token);
        }

        await _context.SaveChangesAsync();
    }

    public async Task UnregisterDeviceAsync(Guid userId, string identifier)
    {
        var webSub = await _context.WebPushSubscriptions
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Endpoint == identifier);

        if (webSub != null) webSub.IsActive = false;

        var mobileToken = await _context.PushNotificationTokens
            .FirstOrDefaultAsync(t => t.UserId == userId && t.DeviceToken == identifier);

        if (mobileToken != null) mobileToken.IsActive = false;

        await _context.SaveChangesAsync();
    }

    private async Task RegisterWithNotificationHubAsync(Guid userId, PushNotificationToken token)
    {
        try
        {
            var connectionString = await _secretProvider.GetSecretAsync("Azure:NotificationHub:ConnectionString");
            var hubName = _configuration["Azure:NotificationHub:HubName"];

            if (string.IsNullOrEmpty(connectionString) || string.IsNullOrEmpty(hubName)) return;

            var hub = NotificationHubClient.CreateClientFromConnectionString(connectionString, hubName);
            var tags = new List<string> { $"user:{userId}", $"tenant:{token.TenantId}" };

            if (token.Platform == "FCM")
            {
                await hub.CreateFcmNativeRegistrationAsync(token.DeviceToken, tags);
            }
            else if (token.Platform == "APNS")
            {
                await hub.CreateAppleNativeRegistrationAsync(token.DeviceToken, tags);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register device token with Azure Notification Hub");
        }
    }
}
