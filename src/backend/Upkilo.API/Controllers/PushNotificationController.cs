using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Upkilo.Core.Interfaces;
using Upkilo.Core.Entities;

namespace Upkilo.API.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class PushNotificationController : ControllerBase
    {
        private readonly IPushNotificationService _pushService;
        private readonly ITenantProvider _tenantProvider;

        public PushNotificationController(IPushNotificationService pushService, ITenantProvider tenantProvider)
        {
            _pushService = pushService;
            _tenantProvider = tenantProvider;
        }

        private Guid GetTenantId() => _tenantProvider.GetTenantId()
            ?? throw new UnauthorizedAccessException("Tenant context not available");

        /// <summary>
        /// Register a device token for push notifications
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> RegisterToken([FromBody] RegisterTokenRequest request)
        {
            var userId = _tenantProvider.GetUserId();
            if (userId == null || userId == Guid.Empty) return Unauthorized();

            var token = new PushNotificationToken
            {
                UserId = userId.Value,
                DeviceToken = request.Token,
                Platform = request.Platform,
                DeviceModel = request.DeviceModel,
                OsVersion = request.OsVersion,
                IsActive = true,
                RegisteredAt = DateTime.UtcNow
            };

            await _pushService.RegisterMobileTokenAsync(userId.Value, token);

            return Ok(new { message = "Device token registered successfully" });
        }

        /// <summary>
        /// Test a push notification for the current user
        /// </summary>
        [HttpPost("test")]
        public async Task<IActionResult> TestNotification()
        {
            var userId = _tenantProvider.GetUserId();
            if (userId == null || userId == Guid.Empty) return Unauthorized();

            await _pushService.SendPushToUserAsync(userId.Value, "Test Notification", "This is a test notification from Upkilo.");
            return Ok(new { message = "Test notification dispatched" });
        }
    }

    public class RegisterTokenRequest
    {
        public string Token { get; set; } = string.Empty;
        public string Platform { get; set; } = "FCM"; // FCM, APNS
        public string? DeviceModel { get; set; }
        public string? OsVersion { get; set; }
    }
}
