using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Interfaces;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data;
using Upkilo.API.Middleware;

namespace Upkilo.API.Controllers;

/// <summary>
/// Clients controller
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class ClientsController : ControllerBase
{
    private readonly ILogger<ClientsController> _logger;
    private readonly IEventService _eventService;
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILoyaltyService _loyaltyService;
    private readonly ICsvExportService _csvExportService;
    private readonly IElasticsearchService? _elasticsearch;
    private readonly IEntitlementService _entitlements;

    public ClientsController(
        ILogger<ClientsController> logger,
        IEventService eventService,
        AppDbContext context,
        ITenantProvider tenantProvider,
        ILoyaltyService loyaltyService,
        ICsvExportService csvExportService,
        IEntitlementService entitlements,
        IElasticsearchService? elasticsearch = null)
    {
        _entitlements = entitlements;
        _logger = logger;
        _eventService = eventService;
        _context = context;
        _tenantProvider = tenantProvider;
        _loyaltyService = loyaltyService;
        _csvExportService = csvExportService;
        _elasticsearch = elasticsearch;
    }

    /// <summary>
    /// S3: GET /clients/smart-search?q={query} — Elasticsearch-backed client search with fuzzy matching.
    /// Falls back to SQL LIKE search when Elasticsearch is unavailable.
    /// </summary>
    [HttpGet("smart-search")]
    public async Task<IActionResult> SmartSearch([FromQuery] string q, [FromQuery] string[]? types = null)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();
        if (string.IsNullOrWhiteSpace(q)) return BadRequest(new { error = "Query is required." });

        // Try Elasticsearch first (S3 — ranked results with fuzzy matching)
        if (_elasticsearch != null)
        {
            try
            {
                var suggestions = await _elasticsearch.AutocompleteAsync(
                    tenantId.Value.ToString("N"),
                    q,
                    types ?? new[] { "client" });

                if (suggestions.Any())
                {
                    return Ok(new
                    {
                        source = "elasticsearch",
                        query = q,
                        count = suggestions.Count(),
                        results = suggestions.Select(s => new { s.Id, s.Text, s.Type, s.Score })
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[S3] Elasticsearch unavailable, falling back to SQL search");
            }
        }

        // Fallback: SQL LIKE search
        var clients = await _context.Clients
            .Where(c => c.TenantId == tenantId.Value
                && (c.FirstName != null && EF.Functions.ILike(c.FirstName, $"%{q}%")
                    || c.LastName != null && EF.Functions.ILike(c.LastName, $"%{q}%")
                    || c.Email != null && EF.Functions.ILike(c.Email, $"%{q}%")
                    || c.Phone != null && c.Phone.Contains(q)))
            .OrderByDescending(c => c.LastVisitAt)
            .Take(20)
            .Select(c => new { c.Id, Text = c.FirstName + " " + c.LastName, Type = "client", Score = 1.0 })
            .ToListAsync();

        return Ok(new { source = "sql_fallback", query = q, count = clients.Count, results = clients });
    }

    /// <summary>
    /// Export all client data to CSV (Data Export Service) — streamed row-by-row to
    /// prevent loading the entire client table into memory.
    /// </summary>
    [HttpGet("export")]
    public async Task ExportClients(CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) { Response.StatusCode = 401; return; }

        Response.ContentType = "text/csv";
        Response.Headers.ContentDisposition = $"attachment; filename=clients_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";

        await using var writer = new System.IO.StreamWriter(Response.Body, System.Text.Encoding.UTF8, leaveOpen: true);
        await writer.WriteLineAsync("Id,FirstName,LastName,Email,Phone,CreatedAt,LifetimeValue");

        await foreach (var c in _context.Clients
            .Where(c => c.TenantId == tenantId)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new { c.Id, c.FirstName, c.LastName, c.Email, c.Phone, c.CreatedAt, c.LifetimeValue })
            .AsAsyncEnumerable().WithCancellation(cancellationToken))
        {
            await writer.WriteLineAsync($"{c.Id},{EscapeCsv(c.FirstName)},{EscapeCsv(c.LastName)},{EscapeCsv(c.Email)},{EscapeCsv(c.Phone)},{c.CreatedAt:O},{c.LifetimeValue}");
        }
    }

    private static string EscapeCsv(string? value)
    {
        if (value == null) return string.Empty;
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    /// <summary>
    /// Get all clients
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetClients(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        [FromQuery] string? search = null)
    {
        var query = _context.Clients.AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            // ILike uses the GIN trigram index (IX_Clients_FullText) — avoids full table scan
            // that LIKE '%term%' causes due to the leading wildcard.
            query = query.Where(c =>
                EF.Functions.ILike(c.FirstName + " " + c.LastName, $"%{search}%") ||
                EF.Functions.ILike(c.Email ?? "", $"%{search}%"));
        }

        var total = await query.Where(c => !c.IsDeleted).CountAsync();
        var clients = await query
            .Where(c => !c.IsDeleted)
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(c => new
            {
                c.Id,
                c.FirstName,
                c.LastName,
                c.Email,
                c.Phone,
                c.CreatedAt,
                c.LastBookingAt,
                c.LifetimeValue,
                c.Tags,
                c.LoyaltyTier
            })
            .ToListAsync();

        return Ok(new
        {
            data = clients,
            page,
            limit,
            total
        });
    }

    /// <summary>
    /// Filter clients by segment criteria
    /// </summary>
    [HttpPost("segment")]
    public async Task<IActionResult> SegmentClients([FromBody] ClientSegmentRequest request)
    {
        var query = _context.Clients.AsQueryable();

        // 1. Min Spend (Lifetime Value)
        if (request.MinSpend.HasValue)
        {
            query = query.Where(c => c.LifetimeValue >= request.MinSpend.Value);
        }

        // 2. Last Visit (Days Ago)
        if (request.MaxDaysSinceLastVisit.HasValue)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-request.MaxDaysSinceLastVisit.Value);
            query = query.Where(c => c.LastBookingAt >= cutoffDate);
        }
        else if (request.MinDaysSinceLastVisit.HasValue)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-request.MinDaysSinceLastVisit.Value);
            query = query.Where(c => c.LastBookingAt <= cutoffDate);
        }

        // 3. Tags
        if (request.Tags != null && request.Tags.Any())
        {
            // Note: EF Core translation for List<string> contains might be provider specific or require specific setup
            // For now assuming PostgreSQL array operations or client-side eval if dataset is small (but we want server-side)
            // Ideally: query.Where(c => c.Tags.Any(t => request.Tags.Contains(t))); 
            // Better for PostgreSQL text[]: 
            foreach (var tag in request.Tags)
            {
                query = query.Where(c => c.Tags.Contains(tag));
            }
        }

        // 4. Loyalty Tier
        if (!string.IsNullOrEmpty(request.LoyaltyTier))
        {
            query = query.Where(c => c.LoyaltyTier == request.LoyaltyTier);
        }

        var total = await query.CountAsync();
        var clients = await query
            .OrderByDescending(c => c.LifetimeValue)
            .Take(100) // Safety limit
            .Select(c => new
            {
                c.Id,
                c.FirstName,
                c.LastName,
                c.Email,
                c.Phone,
                c.CreatedAt,
                c.LastBookingAt,
                c.LifetimeValue,
                c.Tags,
                c.LoyaltyTier
            })
            .ToListAsync();

        return Ok(new { data = clients, total });
    }

    /// <summary>
    /// Get client by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetClient(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var client = await _context.Clients
            .Where(x => x.Id == id && x.TenantId == tenantId)
            .Select(c => new
            {
                c.Id,
                c.FirstName,
                c.LastName,
                c.Email,
                c.Phone,
                c.CreatedAt,
                c.LastBookingAt,
                c.LifetimeValue,
                c.Tags,
                c.LoyaltyTier,
                c.Notes
            })
            .FirstOrDefaultAsync();
        if (client == null) return NotFound();

        return Ok(client);
    }

    /// <summary>
    /// Create client
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateClient([FromBody] CreateClientRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        // max_clients is a published tier boundary (Free 150, Starter 5,000, Growth unlimited)
        // that nothing enforced. Unlike staff and locations it was not even consulted by the
        // downgrade handler, so it was decorative in every direction: displayed on the pricing
        // page and in billing, never able to refuse a record.
        var limitResult = await Upkilo.API.Helpers.SeatLimitGuard.CheckAsync(
            _entitlements, tenantId.Value, FeatureKeys.MaxClients,
            () => _context.Clients.CountAsync(c => c.TenantId == tenantId.Value && !c.IsDeleted),
            "Client", HttpContext.RequestAborted, _logger);
        if (limitResult != null) return limitResult;

        var client = new Client
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId.Value,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email ?? string.Empty,
            Phone = request.Phone,
            CreatedAt = DateTime.UtcNow
        };

        _context.Clients.Add(client);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Client created: {ClientId}", client.Id);

        // Publish Event for Workflow Engine
        await _eventService.PublishAsync("client.created", client, _tenantProvider.GetTenantId() ?? Guid.Empty);

        return CreatedAtAction(nameof(GetClient), new { id = client.Id }, client);
    }

    /// <summary>
    /// Update client
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateClient(Guid id, [FromBody] UpdateClientRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var client = await _context.Clients.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId);
        if (client == null) return NotFound();

        if (request.FirstName != null) client.FirstName = request.FirstName;
        if (request.LastName != null) client.LastName = request.LastName;
        if (request.Email != null) client.Email = request.Email;
        if (request.Phone != null) client.Phone = request.Phone;

        await _context.SaveChangesAsync();

        await _eventService.PublishAsync("client.updated", client, _tenantProvider.GetTenantId() ?? Guid.Empty);

        _logger.LogInformation("Client updated: {ClientId}", id);

        return Ok(client);
    }

    /// <summary>
    /// Delete client (Soft Delete)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteClient(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var client = await _context.Clients.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId);
        if (client == null || client.IsDeleted) return NotFound();

        client.IsDeleted = true;
        client.DeletedAt = DateTime.UtcNow;
        client.DeletedBy = User.FindFirst("id")?.Value;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Client soft-deleted: {ClientId}", id);
        return NoContent();
    }

    /// <summary>
    /// Restore a deleted client
    /// </summary>
    [HttpPost("{id}/restore")]
    public async Task<IActionResult> RestoreDeletedClient(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var client = await _context.Clients.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && x.IsDeleted);
        if (client == null) return NotFound();

        client.IsDeleted = false;
        client.DeletedAt = null;
        client.DeletedBy = null;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Client restored: {ClientId}", id);
        return Ok(new { success = true, message = "Client restored successfully" });
    }

    /// <summary>
    /// Advanced client search with multiple filters and sorting
    /// </summary>
    [HttpPost("search")]
    public async Task<IActionResult> AdvancedSearch([FromBody] AdvancedClientSearchRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var query = _context.Clients
            .Where(c => c.TenantId == tenantId.Value && !c.IsDeleted);

        // 1. String Filters
        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            var s = request.Query.ToLower();
            query = query.Where(c => (c.FirstName + " " + c.LastName).ToLower().Contains(s)
                                     || (c.Email != null && c.Email.ToLower().Contains(s))
                                     || (c.Phone != null && c.Phone.Contains(s)));
        }

        // 2. Numeric Range Filters
        if (request.MinLifetimeValue.HasValue) query = query.Where(c => c.LifetimeValue >= request.MinLifetimeValue.Value);
        if (request.MaxLifetimeValue.HasValue) query = query.Where(c => c.LifetimeValue <= request.MaxLifetimeValue.Value);

        // 3. Date Range Filters
        if (request.LastVisitAfter.HasValue) query = query.Where(c => c.LastVisitAt >= request.LastVisitAfter.Value);
        if (request.LastVisitBefore.HasValue) query = query.Where(c => c.LastVisitAt <= request.LastVisitBefore.Value);
        if (request.CreatedAfter.HasValue) query = query.Where(c => c.CreatedAt >= request.CreatedAfter.Value);

        // 4. Other Filters
        if (request.Tags != null && request.Tags.Any())
        {
            foreach (var tag in request.Tags)
            {
                query = query.Where(c => c.Tags.Contains(tag));
            }
        }
        if (!string.IsNullOrWhiteSpace(request.LoyaltyTier)) query = query.Where(c => c.LoyaltyTier == request.LoyaltyTier);

        // 5. Sorting
        query = request.SortBy?.ToLower() switch
        {
            "firstname" => request.SortDescending ? query.OrderByDescending(c => c.FirstName) : query.OrderBy(c => c.FirstName),
            "lastname" => request.SortDescending ? query.OrderByDescending(c => c.LastName) : query.OrderBy(c => c.LastName),
            "lifetimevalue" => request.SortDescending ? query.OrderByDescending(c => c.LifetimeValue) : query.OrderBy(c => c.LifetimeValue),
            "lastvisit" => request.SortDescending ? query.OrderByDescending(c => c.LastVisitAt) : query.OrderBy(c => c.LastVisitAt),
            "createdat" or _ => request.SortDescending ? query.OrderByDescending(c => c.CreatedAt) : query.OrderBy(c => c.CreatedAt)
        };

        var total = await query.CountAsync();
        var data = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        return Ok(new { data, total, page = request.Page, pageSize = request.PageSize });
    }

    /// <summary>
    /// Bulk delete clients (Soft Delete)
    /// </summary>
    [HttpPost("bulk-delete")]
    public async Task<IActionResult> BulkDeleteClients([FromBody] BulkDeleteRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var clientsToDelete = await _context.Clients
            .Where(c => c.TenantId == tenantId && request.ClientIds.Contains(c.Id) && !c.IsDeleted)
            .ToListAsync();

        foreach (var client in clientsToDelete)
        {
            client.IsDeleted = true;
            client.DeletedAt = DateTime.UtcNow;
            client.DeletedBy = User.FindFirst("id")?.Value;
        }

        if (clientsToDelete.Any())
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Bulk soft-deleted {Count} clients", clientsToDelete.Count);
        }

        return Ok(new { success = true, deletedCount = clientsToDelete.Count });
    }


    /// <summary>
    /// Bulk import clients via CSV
    /// </summary>
    [HttpPost("import")]
    public async Task<IActionResult> ImportClients(IFormFile file)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        if (file == null || file.Length == 0)
            return BadRequest("Please upload a valid CSV file");

        var newClients = new List<Client>();
        var errors = new List<string>();

        using (var reader = new StreamReader(file.OpenReadStream()))
        {
            var header = await reader.ReadLineAsync();
            if (string.IsNullOrEmpty(header)) return BadRequest("File is empty");

            int row = 1;
            while (!reader.EndOfStream)
            {
                row++;
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line)) continue;

                var values = line.Split(',');

                try
                {
                    // Expecting: FirstName, LastName, Email, Phone
                    var firstName = values.Length > 0 ? values[0].Trim() : "Unknown";
                    var lastName = values.Length > 1 ? values[1].Trim() : "";
                    var email = values.Length > 2 ? values[2].Trim() : "";
                    var phone = values.Length > 3 ? values[3].Trim() : "";

                    newClients.Add(new Client
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId.Value,
                        FirstName = firstName,
                        LastName = lastName,
                        Email = email,
                        Phone = phone,
                        CreatedAt = DateTime.UtcNow
                    });
                }
                catch (Exception ex)
                {
                    errors.Add($"Row {row}: {ex.Message}");
                }
            }
        }

        if (newClients.Any())
        {
            await _context.Clients.AddRangeAsync(newClients);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Imported {Count} clients for tenant {TenantId}", newClients.Count, tenantId);
        }

        return Ok(new { success = true, imported = newClients.Count, errors });
    }

    /// <summary>
    /// Get client booking history
    /// </summary>
    [HttpGet("{id}/bookings")]
    public async Task<IActionResult> GetClientBookings(Guid id, [FromQuery] int page = 1, [FromQuery] int limit = 10)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var query = _context.Bookings.Where(b => b.ClientId == id && b.TenantId == tenantId);
        var total = await query.CountAsync();
        var bookings = await query
            .OrderByDescending(b => b.StartTime)
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync();

        return Ok(new { data = bookings, page, limit, total });
    }

    /// <summary>
    /// Get client notes
    /// </summary>
    [HttpGet("{id}/notes")]
    public async Task<IActionResult> GetClientNotes(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var notes = await _context.ClientNotes
            .Where(n => n.ClientId == id && n.TenantId == tenantId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        return Ok(new { data = notes });
    }

    /// <summary>
    /// Add note to client
    /// </summary>
    [HttpPost("{id}/notes")]
    public async Task<IActionResult> CreateNote(Guid id, [FromBody] AddClientNoteRequest request)
    {
        var note = new ClientNote
        {
            Id = Guid.NewGuid(),
            ClientId = id,
            AuthorId = Guid.Parse(User.FindFirst("id")?.Value ?? Guid.Empty.ToString()),
            Content = request.Content,
            IsPrivate = request.IsPrivate,
            Category = request.Category,
            CreatedAt = DateTime.UtcNow
        };

        _context.ClientNotes.Add(note);
        await _context.SaveChangesAsync();

        return Ok(note);
    }

    /// <summary>
    /// Get client communication logs
    /// </summary>
    [HttpGet("{id}/communications")]
    public async Task<IActionResult> GetCommunications(Guid id)
    {
        var logs = await _context.CommunicationLogs
            .Where(l => l.ClientId == id)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();

        return Ok(new { data = logs });
    }

    /// <summary>
    /// Get client loyalty history
    /// </summary>
    [HttpGet("{id}/loyalty")]
    public async Task<IActionResult> GetLoyalty(Guid id)
    {
        var client = await _context.Clients.FirstOrDefaultAsync(x => x.Id == id);
        if (client == null) return NotFound();

        var history = await _loyaltyService.GetHistoryAsync(id);

        return Ok(new
        {
            points = client.LoyaltyPoints,
            tier = client.LoyaltyTier,
            history
        });
    }

    /// <summary>
    /// Adjust client points
    /// </summary>
    [HttpPost("{id}/loyalty/adjust")]
    public async Task<IActionResult> AdjustLoyalty(Guid id, [FromBody] AdjustPointsRequest request)
    {
        if (request.Points > 0)
        {
            await _loyaltyService.AwardPointsAsync(id, request.Points, request.Reason);
        }
        else
        {
            await _loyaltyService.RedeemPointsAsync(id, Math.Abs(request.Points), request.Reason);
        }

        return Ok();
    }

    /// <summary>
    /// Merge duplicate clients
    /// </summary>
    [HttpPost("{id}/merge")]
    public async Task<IActionResult> MergeClients(Guid id, [FromBody] MergeClientsRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var targetClient = await _context.Clients.FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId);
        if (targetClient == null) return NotFound("Target client not found");

        var sourceClients = await _context.Clients
            .Where(c => c.TenantId == tenantId && request.SourceClientIds.Contains(c.Id) && c.Id != id)
            .ToListAsync();

        if (!sourceClients.Any()) return BadRequest("No valid source clients found for merging");

        foreach (var source in sourceClients)
        {
            // 1. Reassign Bookings
            var bookings = await _context.Bookings.Where(b => b.ClientId == source.Id).ToListAsync();
            foreach (var b in bookings) b.ClientId = id;

            // 2. Reassign Payments
            var payments = await _context.Payments.Where(p => p.ClientId == source.Id).ToListAsync();
            foreach (var p in payments) p.ClientId = id;

            // 3. Reassign Notes
            var notes = await _context.ClientNotes.Where(n => n.ClientId == source.Id).ToListAsync();
            foreach (var n in notes) n.ClientId = id;

            // 4. Reassign Communications
            var comms = await _context.CommunicationLogs.Where(l => l.ClientId == source.Id).ToListAsync();
            foreach (var l in comms) l.ClientId = id;

            // 5. Merge Tags
            foreach (var tag in source.Tags)
            {
                if (!targetClient.Tags.Contains(tag)) targetClient.Tags.Add(tag);
            }

            // 6. Merge Custom Fields
            foreach (var field in source.CustomFields)
            {
                if (!targetClient.CustomFields.ContainsKey(field.Key))
                    targetClient.CustomFields[field.Key] = field.Value;
            }

            // 7. Update Lifetime Value
            targetClient.LifetimeValue += source.LifetimeValue;
            targetClient.TotalBookings += source.TotalBookings;

            // 8. Soft delete source
            source.IsDeleted = true;
            source.DeletedAt = DateTime.UtcNow;
            source.DeletedBy = "System-Merge";
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Merged {Count} clients into {TargetId}", sourceClients.Count, id);

        return Ok(new { success = true, mergedCount = sourceClients.Count });
    }

    /// <summary>
    /// Get client loyalty points history
    /// </summary>
    [HttpGet("{id}/loyalty-history")]
    public async Task<IActionResult> GetLoyaltyHistory(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var history = await _context.LoyaltyTransactions
            .Where(t => t.ClientId == id && t.TenantId == tenantId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return Ok(history);
    }

    /// <summary>
    /// Adjust client loyalty points (Admin only)
    /// </summary>
    [HttpPost("{id}/adjust-loyalty")]
    public async Task<IActionResult> AdjustLoyalty(Guid id, [FromBody] AdjustLoyaltyRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var client = await _context.Clients.FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId);
        if (client == null) return NotFound();

        var transaction = new Upkilo.Core.Entities.LoyaltyTransaction
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId.Value,
            ClientId = id,
            Points = request.Points,
            Description = request.Reason,
            TransactionType = LoyaltyTransactionType.Adjustment,
            CreatedAt = DateTime.UtcNow
        };

        client.LoyaltyPoints += request.Points;
        _context.LoyaltyTransactions.Add(transaction);
        await _context.SaveChangesAsync();

        return Ok(new { newBalance = client.LoyaltyPoints, transactionId = transaction.Id });
    }

    /// <summary>
    /// Create a new referral for a client
    /// </summary>
    [HttpPost("{id}/referrals")]
    public async Task<IActionResult> CreateReferral(Guid id, [FromBody] CreateClientReferralRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var referral = new ClientReferral
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId.Value,
            ReferrerClientId = id,
            Email = request.Email,
            Phone = request.Phone,
            Status = ClientReferralStatus.Pending,
            RewardPoints = 100, // Default reward
            CreatedAt = DateTime.UtcNow
        };

        _context.ClientReferrals.Add(referral);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Client referral created for {Email} by {ReferrerId}", request.Email, id);

        return Ok(referral);
    }

    /// <summary>
    /// Get client referrals
    /// </summary>
    [HttpGet("{id}/referrals")]
    public async Task<IActionResult> GetReferrals(Guid id)
    {
        var referrals = await _context.ClientReferrals
            .Where(r => r.ReferrerClientId == id)
            .Select(r => new
            {
                r.Id,
                r.Email,
                r.Status,
                r.RewardPoints,
                r.RewardIssued,
                r.CreatedAt
            })
            .ToListAsync();

        return Ok(referrals);
    }

    /// <summary>
    /// Get client comprehensive activity feed
    /// </summary>
    [HttpGet("{id}/activities")]
    public async Task<IActionResult> GetClientActivityFeed(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var bookings = await _context.Bookings
            .Where(b => b.ClientId == id && b.TenantId == tenantId)
            .Select(b => new { Type = "Booking", Date = b.StartTime, Description = "Booking for " + (b.Service != null ? b.Service.Name : "Unknown"), Status = b.Status.ToString() })
            .ToListAsync();

        var comms = await _context.CommunicationLogs
            .Where(l => l.ClientId == id && l.TenantId == tenantId)
            .Select(l => new { Type = "Communication", Date = l.CreatedAt, Description = l.Type + " " + l.Direction + ": " + l.Subject, Status = l.Status.ToString() })
            .ToListAsync();

        var notes = await _context.ClientNotes
            .Where(n => n.ClientId == id && n.TenantId == tenantId)
            .Select(n => new { Type = "Note", Date = n.CreatedAt, Description = n.Content, Status = n.Category })
            .ToListAsync();

        var activities = bookings.Cast<object>()
            .Concat(comms.Cast<object>())
            .Concat(notes.Cast<object>())
            .OrderByDescending(x => ((dynamic)x).Date)
            .ToList();

        return Ok(activities);
    }

    /// <summary>GET /api/v1/clients/duplicates — detect potential duplicate clients</summary>
    [HttpGet("duplicates")]
    public async Task<IActionResult> DetectDuplicates([FromQuery] int threshold = 80)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var clients = await _context.Clients
            .Where(c => c.TenantId == tenantId && !c.IsDeleted)
            .OrderBy(c => c.Email ?? c.FirstName)
            .Select(c => new
            {
                c.Id,
                c.FirstName,
                c.LastName,
                c.Email,
                c.Phone,
                c.CreatedAt,
                c.TotalBookings,
                c.LifetimeValue
            })
            .ToListAsync();

        var duplicateGroups = new List<object>();
        var processed = new HashSet<Guid>();

        for (int i = 0; i < clients.Count; i++)
        {
            if (processed.Contains(clients[i].Id)) continue;

            var group = new List<object> { clients[i] };
            for (int j = i + 1; j < clients.Count; j++)
            {
                if (processed.Contains(clients[j].Id)) continue;

                int score = 0;
                // Email match (high weight)
                if (!string.IsNullOrEmpty(clients[i].Email) && clients[i].Email == clients[j].Email) score += 60;
                // Phone match (high weight)
                if (!string.IsNullOrEmpty(clients[i].Phone) && clients[i].Phone == clients[j].Phone) score += 50;
                // Name similarity (partial)
                var nameI = $"{clients[i].FirstName} {clients[i].LastName}".Trim().ToLower();
                var nameJ = $"{clients[j].FirstName} {clients[j].LastName}".Trim().ToLower();
                if (!string.IsNullOrEmpty(nameI) && nameI == nameJ) score += 30;
                else if (!string.IsNullOrEmpty(nameI) && nameI.StartsWith(nameJ.Split(' ')[0])) score += 10;

                if (score >= threshold)
                {
                    group.Add(clients[j]);
                    processed.Add(clients[j].Id);
                }
            }

            if (group.Count > 1)
            {
                processed.Add(clients[i].Id);
                duplicateGroups.Add(new
                {
                    groupId = Guid.NewGuid().ToString("N")[..8],
                    clients = group,
                    reason = "Email, phone, or name match",
                });
            }
        }

        return Ok(ApiResponse<object>.Ok(new
        {
            duplicateGroups,
            totalGroups = duplicateGroups.Count,
            totalDuplicateClients = duplicateGroups.Sum(g => ((dynamic)g).clients.Count)
        }));
    }
}

public record MergeClientsRequest(List<Guid> SourceClientIds);

// Appended by duplicate detection implementation


public record ClientSegmentRequest(
    decimal? MinSpend,
    int? MinDaysSinceLastVisit,
    int? MaxDaysSinceLastVisit,
    List<string>? Tags,
    string? LoyaltyTier
);

public record AdjustPointsRequest(int Points, string Reason);

public record AddClientNoteRequest(string Content, bool IsPrivate, string? Category);

public record CreateClientRequest(
    string FirstName,
    string LastName,
    string? Email,
    string? Phone,
    string? Notes,
    bool MarketingConsent,
    bool SmsConsent
);

public record UpdateClientRequest(
    string? FirstName,
    string? LastName,
    string? Email,
    string? Phone,
    string? Notes,
    List<string>? Tags
);

public record AddNoteRequest(string Content, bool IsPinned);

public record BulkDeleteRequest(List<Guid> ClientIds);

public record AdjustLoyaltyRequest(int Points, string Reason);
public record CreateClientReferralRequest(string Email, string? Phone);

public class AdvancedClientSearchRequest
{
    public string? Query { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public List<string>? Tags { get; set; }
    public decimal? MinLifetimeValue { get; set; }
    public decimal? MaxLifetimeValue { get; set; }
    public int? MinLeadScore { get; set; }
    public int? MaxLeadScore { get; set; }
    public DateTime? LastVisitAfter { get; set; }
    public DateTime? LastVisitBefore { get; set; }
    public DateTime? CreatedAfter { get; set; }
    public DateTime? CreatedBefore { get; set; }
    public string? Source { get; set; }
    public string? LoyaltyTier { get; set; }
    public bool? MarketingConsent { get; set; }
    public string? SortBy { get; set; } = "CreatedAt";
    public bool SortDescending { get; set; } = true;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
