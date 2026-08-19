using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Controllers;

/// <summary>
/// Client vehicles and vehicle-class pricing, for tenants whose work is done on a vehicle rather
/// than a person — auto detailing being the vertical this exists for.
///
/// Two things live here because they are one workflow: you cannot quote a detailing job without
/// knowing the vehicle, and the vehicle is worthless as a record if it does not drive the quote.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/vehicles")]
[Authorize]
public class VehiclesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<VehiclesController> _logger;

    public VehiclesController(AppDbContext context, ITenantProvider tenantProvider, ILogger<VehiclesController> logger)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    /// <summary>GET /vehicles?clientId= — vehicles, optionally filtered to one client.</summary>
    [HttpGet]
    public async Task<IActionResult> GetVehicles([FromQuery] Guid? clientId = null)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var query = _context.Vehicles.Where(v => v.TenantId == tenantId && v.IsActive);
        if (clientId.HasValue) query = query.Where(v => v.ClientId == clientId.Value);

        var vehicles = await query
            .OrderBy(v => v.Make).ThenBy(v => v.Model)
            .Select(v => new
            {
                v.Id,
                v.ClientId,
                v.Make,
                v.Model,
                v.Year,
                vehicleClass = v.Class.ToString(),
                v.LicensePlate,
                v.Color,
                v.Notes,
            })
            .AsNoTracking()
            .ToListAsync();

        return Ok(new { data = vehicles });
    }

    /// <summary>
    /// GET /vehicles/{id}/history — every past booking for this vehicle.
    /// This is the reactivation surface: the last service and when it happened is what a
    /// "due again" reminder is built from.
    /// </summary>
    [HttpGet("{id:guid}/history")]
    public async Task<IActionResult> GetVehicleHistory(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var vehicle = await _context.Vehicles
            .FirstOrDefaultAsync(v => v.Id == id && v.TenantId == tenantId);
        if (vehicle == null) return NotFound();

        var history = await _context.Bookings
            .Where(b => b.TenantId == tenantId && b.VehicleId == id)
            .OrderByDescending(b => b.StartTime)
            .Select(b => new
            {
                b.Id,
                b.StartTime,
                b.EndTime,
                status = b.Status.ToString(),
                service = b.Service != null ? b.Service.Name : b.ServiceName,
                b.Price,
            })
            .AsNoTracking()
            .ToListAsync();

        return Ok(new
        {
            data = new
            {
                vehicle = new { vehicle.Id, vehicle.Make, vehicle.Model, vehicle.Year, vehicleClass = vehicle.Class.ToString() },
                lastServicedAt = history.FirstOrDefault()?.StartTime,
                totalVisits = history.Count,
                history,
            }
        });
    }

    /// <summary>POST /vehicles — add a vehicle to a client.</summary>
    [HttpPost]
    public async Task<IActionResult> CreateVehicle([FromBody] CreateVehicleRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        // Checked rather than assumed: ClientId arrives from the caller, and without this a
        // vehicle could be attached to a client belonging to another tenant.
        var clientExists = await _context.Clients
            .AnyAsync(c => c.Id == request.ClientId && c.TenantId == tenantId);
        if (!clientExists) return NotFound(new { error = "Client not found" });

        if (!Enum.TryParse<VehicleClass>(request.VehicleClass, ignoreCase: true, out var vehicleClass))
            return BadRequest(new { error = $"Unknown vehicle class '{request.VehicleClass}'. Valid values: {string.Join(", ", Enum.GetNames<VehicleClass>())}." });

        var vehicle = new Vehicle
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId.Value,
            ClientId = request.ClientId,
            Make = request.Make?.Trim(),
            Model = request.Model?.Trim(),
            Year = request.Year,
            Class = vehicleClass,
            LicensePlate = request.LicensePlate?.Trim(),
            Color = request.Color?.Trim(),
            Notes = request.Notes?.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

        _context.Vehicles.Add(vehicle);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Vehicle {VehicleId} added for client {ClientId}", vehicle.Id, vehicle.ClientId);

        return CreatedAtAction(nameof(GetVehicleHistory), new { id = vehicle.Id }, new { data = new { vehicle.Id } });
    }

    /// <summary>
    /// GET /vehicles/quote?serviceId=&amp;vehicleClass= — the price and duration for a service on
    /// a given vehicle class.
    ///
    /// Falls back to the service's own price and duration when no class-specific row exists, so a
    /// tenant only has to price the classes that genuinely differ.
    /// </summary>
    [HttpGet("quote")]
    public async Task<IActionResult> GetQuote([FromQuery] Guid serviceId, [FromQuery] string vehicleClass)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        if (!Enum.TryParse<VehicleClass>(vehicleClass, ignoreCase: true, out var parsedClass))
            return BadRequest(new { error = $"Unknown vehicle class '{vehicleClass}'." });

        var service = await _context.Services
            .FirstOrDefaultAsync(s => s.Id == serviceId && s.TenantId == tenantId);
        if (service == null) return NotFound(new { error = "Service not found" });

        var classPrice = await _context.ServiceVehiclePrices
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.ServiceId == serviceId && p.VehicleClass == parsedClass);

        return Ok(new
        {
            data = new
            {
                serviceId,
                service = service.Name,
                vehicleClass = parsedClass.ToString(),
                price = classPrice?.Price ?? service.Price,
                durationMinutes = classPrice?.DurationMinutes ?? service.DurationMinutes,
                currency = service.Currency,
                // Stated explicitly so the booking UI can show "standard price" rather than
                // implying the quote was tailored when no row existed for this class.
                isClassSpecific = classPrice != null,
                // Deposit and refund terms travel with the quote — an expensive detailing job is
                // exactly where a customer needs both before they commit.
                depositAmount = service.DepositAmount,
                requiresPayment = service.RequiresPayment,
                refundPolicy = new
                {
                    fullRefundHours = service.FullRefundHours,
                    partialRefundHours = service.PartialRefundHours,
                    partialRefundPercent = service.PartialRefundPercent,
                },
            }
        });
    }

    /// <summary>PUT /vehicles/pricing — set (or clear) the price for one service on one class.</summary>
    [HttpPut("pricing")]
    [Authorize(Roles = "Owner,Admin")]
    public async Task<IActionResult> SetVehiclePricing([FromBody] SetVehiclePricingRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        if (!Enum.TryParse<VehicleClass>(request.VehicleClass, ignoreCase: true, out var parsedClass))
            return BadRequest(new { error = $"Unknown vehicle class '{request.VehicleClass}'." });

        if (request.Price < 0 || request.DurationMinutes <= 0)
            return BadRequest(new { error = "Price cannot be negative and duration must be greater than zero." });

        var service = await _context.Services
            .FirstOrDefaultAsync(s => s.Id == request.ServiceId && s.TenantId == tenantId);
        if (service == null) return NotFound(new { error = "Service not found" });

        var existing = await _context.ServiceVehiclePrices
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.ServiceId == request.ServiceId && p.VehicleClass == parsedClass);

        if (existing != null)
        {
            existing.Price = request.Price;
            existing.DurationMinutes = request.DurationMinutes;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _context.ServiceVehiclePrices.Add(new ServiceVehiclePrice
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId.Value,
                ServiceId = request.ServiceId,
                VehicleClass = parsedClass,
                Price = request.Price,
                DurationMinutes = request.DurationMinutes,
                CreatedAt = DateTime.UtcNow,
            });
        }

        await _context.SaveChangesAsync();
        return Ok(new { success = true });
    }

    /// <summary>GET /vehicles/pricing/{serviceId} — the full class matrix for one service.</summary>
    [HttpGet("pricing/{serviceId:guid}")]
    public async Task<IActionResult> GetVehiclePricing(Guid serviceId)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var service = await _context.Services
            .FirstOrDefaultAsync(s => s.Id == serviceId && s.TenantId == tenantId);
        if (service == null) return NotFound(new { error = "Service not found" });

        var rows = await _context.ServiceVehiclePrices
            .Where(p => p.TenantId == tenantId && p.ServiceId == serviceId)
            .AsNoTracking()
            .ToListAsync();

        // Every class is returned, priced or not, so the UI renders a complete matrix and the
        // tenant can see at a glance which classes are still falling back to the base price.
        var matrix = Enum.GetValues<VehicleClass>().Select(c =>
        {
            var row = rows.FirstOrDefault(r => r.VehicleClass == c);
            return new
            {
                vehicleClass = c.ToString(),
                price = row?.Price ?? service.Price,
                durationMinutes = row?.DurationMinutes ?? service.DurationMinutes,
                isClassSpecific = row != null,
            };
        });

        return Ok(new { data = new { serviceId, service = service.Name, currency = service.Currency, matrix } });
    }
}

public record CreateVehicleRequest(
    Guid ClientId,
    string? Make,
    string? Model,
    int? Year,
    string VehicleClass,
    string? LicensePlate,
    string? Color,
    string? Notes
);

public record SetVehiclePricingRequest(
    Guid ServiceId,
    string VehicleClass,
    decimal Price,
    int DurationMinutes
);
