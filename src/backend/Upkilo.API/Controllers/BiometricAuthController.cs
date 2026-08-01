using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Security.Claims;
using System.Text.Json;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;
using Upkilo.Infrastructure.Services;

namespace Upkilo.API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/auth/biometrics")]
public class BiometricAuthController : ControllerBase
{
    private readonly IFido2 _fido2;
    private readonly AppDbContext _context;
    private readonly IDistributedCache _cache;
    private readonly IAuthService _authService;
    private readonly ILogger<BiometricAuthController> _logger;

    public BiometricAuthController(
        IFido2 fido2,
        AppDbContext context,
        IDistributedCache cache,
        IAuthService authService,
        ILogger<BiometricAuthController> logger)
    {
        _fido2 = fido2;
        _context = context;
        _cache = cache;
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("register-options")]
    [Authorize]
    public async Task<IActionResult> MakeCredentialOptions()
    {
        try
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdStr, out var userId)) return Unauthorized();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return NotFound("User not found");

            var existingKeys = await _context.UserPasskeys
                .Where(p => p.UserId == userId)
                .Select(p => new PublicKeyCredentialDescriptor(p.CredentialId))
                .ToListAsync();

            var userBytes = System.Text.Encoding.UTF8.GetBytes(user.Id.ToString());

            var fidoUser = new Fido2User
            {
                DisplayName = $"{user.FirstName} {user.LastName}",
                Name = user.Email,
                Id = userBytes // Expected byte[]
            };

            var authenticatorSelection = new AuthenticatorSelection
            {
                RequireResidentKey = false,
                UserVerification = UserVerificationRequirement.Preferred
            };

            var options = _fido2.RequestNewCredential(fidoUser, existingKeys, authenticatorSelection, AttestationConveyancePreference.None);

            // Temporarily store the options to verify in the next step
            await _cache.SetStringAsync($"fido2:reg:{userId}", options.ToJson(), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });

            return Ok(options);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error creating credential options");
            return BadRequest(new { message = "Error creating credential options" });
        }
    }

    [HttpPost("register")]
    [Authorize]
    public async Task<IActionResult> MakeCredential([FromBody] AuthenticatorAttestationRawResponse attestationResponse)
    {
        try
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdStr, out var userId)) return Unauthorized();

            var jsonOptions = await _cache.GetStringAsync($"fido2:reg:{userId}");
            if (string.IsNullOrEmpty(jsonOptions))
                return BadRequest(new { message = "Registration session expired or invalid" });

            var options = CredentialCreateOptions.FromJson(jsonOptions);

            var cbParams = new IsCredentialIdUniqueToUserAsyncDelegate(async (args, cancellationToken) =>
            {
                return !await _context.UserPasskeys.AnyAsync(p => p.CredentialId == args.CredentialId, cancellationToken);
            });

            var success = await _fido2.MakeNewCredentialAsync(attestationResponse, options, cbParams);

            if (success.Result != null)
            {
                var passkey = new UserPasskey
                {
                    UserId = userId,
                    CredentialId = success.Result.CredentialId,
                    PublicKey = success.Result.PublicKey,
                    UserHandle = success.Result.User.Id,
                    SignatureCounter = success.Result.Counter,
                    CredentialType = success.Result.CredType,
                    Aaguid = success.Result.Aaguid.ToString(),
                    RegDate = DateTime.UtcNow,
                    RegOrigin = options.Rp.Id
                };

                _context.UserPasskeys.Add(passkey);
                await _context.SaveChangesAsync();

                await _cache.RemoveAsync($"fido2:reg:{userId}");
                return Ok(new { success = true, message = "Biometric credential registered successfully." });
            }

            return BadRequest(new { message = "Error verifying credential", errorMessage = success.ErrorMessage });
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error making credential");
            return BadRequest(new { message = "Error adding biometric credential", error = e.Message });
        }
    }

    // Class for request dto
    public class AssertOptionsRequest
    {
        public string Email { get; set; } = string.Empty;
    }

    [HttpPost("verify-options")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAssertionOptions([FromBody] AssertOptionsRequest request)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null) return BadRequest(new { message = "User not found" });

            var existingCredentials = await _context.UserPasskeys
                .Where(p => p.UserId == user.Id)
                .Select(p => new PublicKeyCredentialDescriptor(p.CredentialId))
                .ToListAsync();

            if (!existingCredentials.Any())
                return BadRequest(new { message = "No biometric credentials registered for this user." });

            var options = _fido2.GetAssertionOptions(
                existingCredentials,
                UserVerificationRequirement.Preferred
            );

            // Temporarily store the options 
            await _cache.SetStringAsync($"fido2:auth:{request.Email}", options.ToJson(), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });

            return Ok(options);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error creating assertion options");
            return BadRequest(new { message = "Error creating login options" });
        }
    }

    [HttpPost("verify")]
    [AllowAnonymous]
    public async Task<IActionResult> MakeAssertion([FromBody] AuthenticatorAssertionRawResponse clientResponse, [FromQuery] string email)
    {
        try
        {
            var jsonOptions = await _cache.GetStringAsync($"fido2:auth:{email}");
            if (string.IsNullOrEmpty(jsonOptions))
                return BadRequest(new { message = "Login session expired or invalid" });

            var options = Fido2NetLib.AssertionOptions.FromJson(jsonOptions);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) return BadRequest(new { message = "User not found" });

            var passkey = await _context.UserPasskeys.FirstOrDefaultAsync(p => p.CredentialId == clientResponse.Id);
            if (passkey == null) return BadRequest(new { message = "Unknown credential" });

            var cbParams = new IsUserHandleOwnerOfCredentialIdAsync(async (args, cancellationToken) =>
            {
                return passkey.UserHandle.SequenceEqual(args.UserHandle);
            });

            var success = await _fido2.MakeAssertionAsync(clientResponse, options, passkey.PublicKey, passkey.SignatureCounter, cbParams);

            if (success.Status == "ok")
            {
                // Update counter
                passkey.SignatureCounter = success.Counter;
                await _context.SaveChangesAsync();
                await _cache.RemoveAsync($"fido2:auth:{email}");

                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var userAgent = Request.Headers["User-Agent"].ToString();

                var authResult = await _authService.LoginWithBiometricAsync(user.Id, ipAddress, userAgent);
                if (authResult.Success)
                {
                    return Ok(authResult);
                }

                return BadRequest(new { message = authResult.Message });
            }

            return BadRequest(new { message = "Biometric verification failed", error = success.ErrorMessage });
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Assertion error");
            return BadRequest(new { message = "Error verifying biometric login", error = e.Message });
        }
    }
}
