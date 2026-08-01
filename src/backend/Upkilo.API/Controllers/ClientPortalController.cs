using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace Upkilo.API.Controllers;

/// <summary>
/// Client portal controller — self-service features for end-clients.
/// Uses real database queries against Booking, Client, Service, Payment, and LoyaltyBalance entities.
/// </summary>
[ApiController]
[Route("api/client-portal")]
public class ClientPortalController : ControllerBase
{
    private readonly ILogger<ClientPortalController> _logger;
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;
    private readonly IBookingService _bookingService;
    private readonly IInvoiceService _invoiceService;

    public ClientPortalController(
        ILogger<ClientPortalController> logger,
        AppDbContext context,
        ITenantProvider tenantProvider,
        IConfiguration configuration,
        IEmailService emailService,
        IBookingService bookingService,
        IInvoiceService invoiceService)
    {
        _logger = logger;
        _context = context;
        _tenantProvider = tenantProvider;
        _configuration = configuration;
        _emailService = emailService;
        _bookingService = bookingService;
        _invoiceService = invoiceService;
    }

    /// <summary>
    /// Client login with magic link
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> RequestMagicLink([FromBody] MagicLinkRequest request)
    {
        // Verify the client exists for this business
        var tenantId = _tenantProvider.GetTenantId();
        var client = await _context.Clients
            .FirstOrDefaultAsync(c => c.Email == request.Email &&
                (tenantId == null || c.TenantId == tenantId));

        if (client != null)
        {
            // Secure random token
            var tokenStr = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32))
                .Replace("+", "-").Replace("/", "_").Replace("=", "");

            var magicToken = new MagicLinkToken
            {
                Id = Guid.NewGuid(),
                TenantId = client.TenantId,
                ClientId = client.Id,
                Email = client.Email,
                Token = tokenStr,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                IsUsed = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.MagicLinkTokens.Add(magicToken);
            await _context.SaveChangesAsync();

            var appUrl = (_configuration["APP_URL"] ?? "https://app.upkilo.com").TrimEnd('/');
            var magicLink = $"{appUrl}/portal/verify?token={tokenStr}&email={client.Email}";

            await _emailService.SendSystemEmailAsync(
                client.Email,
                "Sign in to your Client Portal - Upkilo",
                $@"<h2>Login to Your Portal</h2>
                   <p>Hi {client.FirstName},</p>
                   <p>Click the button below to sign in to your client portal. This link expires in 15 minutes.</p>
                   <p><a href=""{magicLink}"" style=""background-color:#4F46E5;color:white;padding:12px 24px;text-decoration:none;border-radius:6px;font-weight:bold;"">Sign In Now</a></p>
                   <p>If you didn't request this, you can safely ignore this email.</p>");
        }

        _logger.LogInformation("Magic link requested for {Email}", request.Email);

        // Always return success to prevent email enumeration
        return Ok(new
        {
            success = true,
            message = "If an account exists, a login link has been sent to your email."
        });
    }

    private string GenerateClientJwtToken(Client client, DateTime expires)
    {
        var jwtSecret = _configuration["Jwt:Secret"] ?? throw new InvalidOperationException("Jwt:Secret is missing from configuration");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("client_id", client.Id.ToString()),
            new Claim(ClaimTypes.Email, client.Email),
            new Claim("tenant_id", client.TenantId.ToString()),
            new Claim("portal_access", "true"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Verify magic link token
    /// </summary>
    [HttpPost("verify")]
    public async Task<IActionResult> VerifyMagicLink([FromBody] VerifyMagicLinkRequest request)
    {
        try
        {
            var magicToken = await _context.MagicLinkTokens
                .FirstOrDefaultAsync(m => m.Token == request.Token && m.Email == request.Email && !m.IsDeleted);

            if (magicToken == null || magicToken.IsUsed || magicToken.ExpiresAt < DateTime.UtcNow)
            {
                return Unauthorized(new { success = false, message = "Invalid, used, or expired link." });
            }

            magicToken.IsUsed = true;
            magicToken.UpdatedAt = DateTime.UtcNow;

            var client = await _context.Clients
                .FirstOrDefaultAsync(c => c.Id == magicToken.ClientId && !c.IsDeleted);

            if (client == null)
                return Unauthorized(new { success = false, message = "Client not found." });

            await _context.SaveChangesAsync();

            // Generate a long-lived session token (30 days)
            var sessionToken = GenerateClientJwtToken(client, DateTime.UtcNow.AddDays(30));

            return Ok(new
            {
                success = true,
                token = sessionToken,
                expiresAt = DateTime.UtcNow.AddDays(30).ToString("o"),
                client = new
                {
                    client.Id,
                    name = $"{client.FirstName} {client.LastName}",
                    client.Email
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token verification failed");
            return Unauthorized(new { success = false, message = "Invalid or expired link." });
        }
    }

    // ─── Profile ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Get client's profile
    /// </summary>
    [HttpGet("profile")]
    [Authorize(Policy = "ClientPortal")]
    public async Task<IActionResult> GetClientProfile()
    {
        // Extract clientId from JWT claims (set during magic link verification)
        var clientIdStr = User.FindFirst("client_id")?.Value;
        if (!Guid.TryParse(clientIdStr, out var clientId))
            return Unauthorized();

        var client = await _context.Clients
            .Include(c => c.Tenant)
            .FirstOrDefaultAsync(c => c.Id == clientId && !c.IsDeleted);

        if (client == null) return NotFound();

        return Ok(new
        {
            client.Id,
            client.FirstName,
            client.LastName,
            client.Email,
            client.Phone,
            client.AvatarUrl,
            client.CreatedAt,
            business = new
            {
                name = client.Tenant?.Name,
                logo = client.Tenant?.LogoUrl,
                primaryColor = client.Tenant?.PrimaryColor ?? "#06B6D4"
            }
        });
    }

    /// <summary>
    /// Update client profile
    /// </summary>
    [HttpPut("profile")]
    [Authorize(Policy = "ClientPortal")]
    public async Task<IActionResult> UpdateClientProfile([FromBody] UpdateClientProfileRequest request)
    {
        var clientIdStr = User.FindFirst("client_id")?.Value;
        if (!Guid.TryParse(clientIdStr, out var clientId))
            return Unauthorized();

        var client = await _context.Clients
            .FirstOrDefaultAsync(c => c.Id == clientId && !c.IsDeleted);

        if (client == null) return NotFound();

        if (request.FirstName != null) client.FirstName = request.FirstName;
        if (request.LastName != null) client.LastName = request.LastName;
        if (request.Phone != null) client.Phone = request.Phone;
        client.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Client profile updated: {ClientId}", clientId);

        return Ok(new { success = true });
    }

    // ─── Appointments ──────────────────────────────────────────────────────────

    /// <summary>
    /// Get client's upcoming appointments
    /// </summary>
    [HttpGet("appointments/upcoming")]
    [Authorize(Policy = "ClientPortal")]
    public async Task<IActionResult> GetUpcomingAppointments()
    {
        var clientIdStr = User.FindFirst("client_id")?.Value;
        if (!Guid.TryParse(clientIdStr, out var clientId))
            return Unauthorized();

        var appointments = await _context.Bookings
            .Include(b => b.Service)
            .Include(b => b.Staff)
            .Where(b => b.ClientId == clientId && !b.IsDeleted &&
                b.StartTime >= DateTime.UtcNow &&
                (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Pending))
            .OrderBy(b => b.StartTime)
            .Select(b => new
            {
                b.Id,
                tenantSlug = b.Tenant != null ? b.Tenant.Slug : "",
                serviceId = b.ServiceId,
                staffId = b.StaffId,
                date = b.StartTime.ToString("yyyy-MM-dd"),
                time = b.StartTime.ToString("HH:mm"),
                service = b.Service != null ? b.Service.Name : "Unknown",
                staff = b.Staff != null ? $"{b.Staff.FirstName} {b.Staff.LastName}" : "Any available",
                duration = b.Service != null ? b.Service.DurationMinutes : 0,
                price = b.Price,
                status = b.Status.ToString().ToLower(),
                canCancel = b.StartTime > DateTime.UtcNow.AddHours(24),
                canReschedule = b.StartTime > DateTime.UtcNow.AddHours(24)
            })
            .ToListAsync();

        return Ok(new { data = appointments });
    }

    /// <summary>
    /// Get client's appointment history
    /// </summary>
    [HttpGet("appointments/history")]
    [Authorize(Policy = "ClientPortal")]
    public async Task<IActionResult> GetAppointmentHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var clientIdStr = User.FindFirst("client_id")?.Value;
        if (!Guid.TryParse(clientIdStr, out var clientId))
            return Unauthorized();

        var query = _context.Bookings
            .Include(b => b.Service)
            .Include(b => b.Staff)
            .Include(b => b.Payments)
            .Where(b => b.ClientId == clientId && !b.IsDeleted &&
                (b.Status == BookingStatus.Completed || b.Status == BookingStatus.Cancelled || b.Status == BookingStatus.NoShow));

        var total = await query.CountAsync();

        var appointments = await query
            .OrderByDescending(b => b.StartTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new
            {
                b.Id,
                date = b.StartTime.ToString("yyyy-MM-dd"),
                time = b.StartTime.ToString("HH:mm"),
                service = b.Service != null ? b.Service.Name : "Unknown",
                staff = b.Staff != null ? $"{b.Staff.FirstName} {b.Staff.LastName}" : "Unknown",
                price = b.Price,
                status = b.Status.ToString().ToLower(),
                notes = b.Notes,
                cancellationReason = b.CancellationReason,
                hasReceipt = b.Payments.Any(p => p.Status == PaymentStatus.Succeeded),
                hasReview = _context.ExternalReviews.Any(r => r.BookingId == b.Id && !r.IsDeleted)
            })
            .ToListAsync();

        return Ok(new { data = appointments, total, page, pageSize });
    }

    /// <summary>
    /// Cancel an appointment
    /// </summary>
    [HttpPost("appointments/{id}/cancel")]
    [Authorize(Policy = "ClientPortal")]
    public async Task<IActionResult> CancelAppointment(Guid id, [FromBody] CancelAppointmentRequest request)
    {
        var clientIdStr = User.FindFirst("client_id")?.Value;
        if (!Guid.TryParse(clientIdStr, out var clientId))
            return Unauthorized();

        var booking = await _context.Bookings
            .FirstOrDefaultAsync(b => b.Id == id && b.ClientId == clientId && !b.IsDeleted);

        if (booking == null) return NotFound();

        if (booking.StartTime <= DateTime.UtcNow.AddHours(24))
            return BadRequest(new { error = "Cannot cancel within 24 hours of appointment." });

        booking.Status = BookingStatus.Cancelled;
        booking.CancellationReason = request?.Reason;
        booking.CancelledAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Client cancelled appointment: {AppointmentId}", id);

        return Ok(new
        {
            success = true,
            message = "Your appointment has been cancelled."
        });
    }

    /// <summary>
    /// Reschedule an appointment
    /// </summary>
    [HttpPost("appointments/{id}/reschedule")]
    [Authorize(Policy = "ClientPortal")]
    public async Task<IActionResult> RescheduleAppointment(Guid id, [FromBody] RescheduleAppointmentRequest request)
    {
        var clientIdStr = User.FindFirst("client_id")?.Value;
        if (!Guid.TryParse(clientIdStr, out var clientId))
            return Unauthorized();

        var tenantIdStr = User.FindFirst("tenant_id")?.Value;
        if (!Guid.TryParse(tenantIdStr, out var tenantId))
            return Unauthorized();

        try
        {
            if (!DateTime.TryParse($"{request.NewDate}T{request.NewTime}", out var newStart))
                return BadRequest(new { error = "Invalid date/time format." });

            var booking = await _bookingService.RescheduleBookingAsync(tenantId, id, newStart, null, null, bypassCodeCheck: true);

            _logger.LogInformation("Client rescheduled appointment: {AppointmentId}", id);

            return Ok(new
            {
                success = true,
                newDate = request.NewDate,
                newTime = request.NewTime,
                message = "Your appointment has been rescheduled."
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Client reschedule failed for appointment {AppointmentId}", id);
            return StatusCode(500, new { error = "An unexpected error occurred." });
        }
    }

    // ─── Payments & Invoices ───────────────────────────────────────────────────

    /// <summary>
    /// Get client's invoices (from payments)
    /// </summary>
    [HttpGet("invoices")]
    [Authorize(Policy = "ClientPortal")]
    public async Task<IActionResult> GetInvoices([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var clientIdStr = User.FindFirst("client_id")?.Value;
        if (!Guid.TryParse(clientIdStr, out var clientId))
            return Unauthorized();

        var tenantIdStr = User.FindFirst("tenant_id")?.Value;
        if (!Guid.TryParse(tenantIdStr, out var tenantId))
            return Unauthorized();

        var invoices = await _invoiceService.GetClientInvoicesAsync(tenantId, clientId, page, pageSize);

        // We count total for pagination
        var total = await _context.Invoices.CountAsync(i => i.ClientId == clientId && !i.IsDeleted);

        var result = invoices.Select(i => new
        {
            i.Id,
            invoiceNumber = i.InvoiceNumber,
            date = i.IssueDate.ToString("yyyy-MM-dd"),
            amount = i.TotalAmount,
            currency = i.Currency,
            status = i.Status.ToString().ToLower()
        });

        return Ok(new { data = result, total, page, pageSize });
    }

    // ─── Services & Booking ────────────────────────────────────────────────────

    /// <summary>
    /// Get available services for booking
    /// </summary>
    [HttpGet("services")]
    public async Task<IActionResult> GetAvailableServices([FromQuery] string? businessSlug)
    {
        var tenantId = _tenantProvider.GetTenantId();

        var query = _context.Services
            .Where(s => s.IsActive && !s.IsDeleted);

        if (tenantId.HasValue)
            query = query.Where(s => s.TenantId == tenantId.Value);

        var services = await query
            .OrderBy(s => s.Category).ThenBy(s => s.Name)
            .Select(s => new
            {
                s.Id,
                s.Name,
                s.Description,
                duration = s.DurationMinutes,
                s.Price,
                s.Category
            })
            .ToListAsync();

        return Ok(new { data = services });
    }

    /// <summary>
    /// Create a booking
    /// </summary>
    [HttpPost("book")]
    public async Task<IActionResult> CreateBooking([FromBody] CreateClientBookingRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return BadRequest(new { error = "Business context not found." });

        if (!DateTime.TryParse($"{request.Date}T{request.Time}", out var startTime))
            return BadRequest(new { error = "Invalid date/time." });

        var service = await _context.Services.FirstOrDefaultAsync(x => x.Id == request.ServiceId);
        if (service == null) return BadRequest(new { error = "Service not found." });

        // Find or create client
        Guid clientId;
        if (request.ClientInfo != null)
        {
            var existingClient = await _context.Clients
                .FirstOrDefaultAsync(c => c.Email == request.ClientInfo.Email && c.TenantId == tenantId.Value && !c.IsDeleted);

            if (existingClient != null)
            {
                clientId = existingClient.Id;
            }
            else
            {
                var newClient = new Client
                {
                    TenantId = tenantId.Value,
                    FirstName = request.ClientInfo.FirstName,
                    LastName = request.ClientInfo.LastName,
                    Email = request.ClientInfo.Email,
                    Phone = request.ClientInfo.Phone
                };
                _context.Clients.Add(newClient);
                await _context.SaveChangesAsync();
                clientId = newClient.Id;
            }
        }
        else
        {
            // Try from JWT
            var clientIdStr = User.FindFirst("client_id")?.Value;
            if (!Guid.TryParse(clientIdStr, out clientId))
                return BadRequest(new { error = "Client information required." });
        }

        var booking = new Booking
        {
            TenantId = tenantId.Value,
            ClientId = clientId,
            ServiceId = request.ServiceId,
            StaffId = request.StaffId,
            StartTime = startTime,
            EndTime = startTime.AddMinutes(service.DurationMinutes),
            Price = service.Price,
            Notes = request.Notes,
            Source = BookingSource.Website
        };

        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Client booking created: {BookingId}", booking.Id);

        return Ok(new
        {
            booking.Id,
            confirmationNumber = $"UPK-{booking.Id.ToString()[..8].ToUpper()}",
            service = service.Name,
            date = request.Date,
            time = request.Time,
            status = "confirmed",
            message = "Your booking has been confirmed!"
        });
    }

    // ─── Loyalty / Rewards ─────────────────────────────────────────────────────

    /// <summary>
    /// Get loyalty points and rewards
    /// </summary>
    [HttpGet("rewards")]
    [Authorize(Policy = "ClientPortal")]
    public async Task<IActionResult> GetRewards()
    {
        var clientIdStr = User.FindFirst("client_id")?.Value;
        if (!Guid.TryParse(clientIdStr, out var clientId))
            return Unauthorized();

        var balance = await _context.Set<LoyaltyBalance>()
            .FirstOrDefaultAsync(lb => lb.ClientId == clientId && !lb.IsDeleted);

        return Ok(new
        {
            points = balance?.TotalPoints ?? 0,
            tier = balance?.CurrentTier ?? "Bronze",
            lifetimePoints = balance?.LifetimePoints ?? 0
        });
    }

    // ─── Messaging ────────────────────────────────────────────────────────────

    /// <summary>
    /// Get client's message thread with the business
    /// </summary>
    [HttpGet("messages")]
    [Authorize(Policy = "ClientPortal")]
    public async Task<IActionResult> GetMessages([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var clientIdStr = User.FindFirst("client_id")?.Value;
        if (!Guid.TryParse(clientIdStr, out var clientId))
            return Unauthorized();

        var query = _context.CommunicationLogs
            .Where(m => m.ClientId == clientId && !m.IsDeleted)
            .OrderByDescending(m => m.CreatedAt);

        var total = await query.CountAsync();
        var messages = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new
            {
                m.Id,
                m.Subject,
                m.Body,
                m.Direction,
                m.Status,
                m.CreatedAt
            })
            .ToListAsync();

        return Ok(new { data = messages, total, page, pageSize });
    }

    /// <summary>
    /// Send a message to the business
    /// </summary>
    [HttpPost("messages")]
    [Authorize(Policy = "ClientPortal")]
    public async Task<IActionResult> SendMessage([FromBody] SendClientMessageRequest request)
    {
        var clientIdStr = User.FindFirst("client_id")?.Value;
        if (!Guid.TryParse(clientIdStr, out var clientId))
            return Unauthorized();

        var client = await _context.Clients.FirstOrDefaultAsync(c => c.Id == clientId && !c.IsDeleted);
        if (client == null) return NotFound();

        var message = new CommunicationLog
        {
            Id = Guid.NewGuid(),
            TenantId = client.TenantId,
            ClientId = clientId,
            Type = CommunicationType.InApp,
            Direction = CommunicationDirection.Inbound,
            Subject = request.Subject,
            Body = request.Message,
            Status = CommunicationStatus.Delivered,
            CreatedAt = DateTime.UtcNow
        };

        _context.CommunicationLogs.Add(message);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Client message received from {ClientId}: {Subject}", clientId, request.Subject);

        return Ok(new
        {
            success = true,
            messageId = message.Id,
            message = "Your message has been sent to the business."
        });
    }
}

// Request DTOs
public class MagicLinkRequest
{
    public string Email { get; set; } = string.Empty;
    public string BusinessSlug { get; set; } = string.Empty;
}

public class VerifyMagicLinkRequest
{
    public string Token { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class UpdateClientProfileRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Phone { get; set; }
}

public class CancelAppointmentRequest
{
    public string? Reason { get; set; }
}

public class RescheduleAppointmentRequest
{
    public string NewDate { get; set; } = string.Empty;
    public string NewTime { get; set; } = string.Empty;
}

public class CreateClientBookingRequest
{
    public Guid ServiceId { get; set; }
    public Guid? StaffId { get; set; }
    public string Date { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public ClientInfo? ClientInfo { get; set; }
}

public class ClientInfo
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
}

public class RedeemRewardRequest
{
    public string RewardId { get; set; } = string.Empty;
}

public class SendClientMessageRequest
{
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
