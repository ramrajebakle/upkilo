using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Controllers;

/// <summary>
/// Credits controller for managing client credit balances
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class CreditsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<CreditsController> _logger;

    public CreditsController(
        AppDbContext context,
        ITenantProvider tenantProvider,
        ILogger<CreditsController> logger)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    /// <summary>
    /// Get client's credit balance
    /// </summary>
    [HttpGet("client/{clientId}")]
    public async Task<IActionResult> GetClientBalance(Guid clientId)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var client = await _context.Clients
            .FirstOrDefaultAsync(c => c.Id == clientId && c.TenantId == tenantId);

        if (client == null) return NotFound();

        // Calculate balance from transactions
        var balance = await _context.Set<CreditTransaction>()
            .Where(ct => ct.ClientId == clientId && ct.TenantId == tenantId)
            .SumAsync(ct => ct.Amount);

        return Ok(new { clientId, balance, currency = "USD" });
    }

    /// <summary>
    /// Get client's credit transaction history
    /// </summary>
    [HttpGet("client/{clientId}/history")]
    public async Task<IActionResult> GetTransactionHistory(Guid clientId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var query = _context.Set<CreditTransaction>()
            .Where(ct => ct.ClientId == clientId && ct.TenantId == tenantId);

        var total = await query.CountAsync();
        var transactions = await query
            .OrderByDescending(ct => ct.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ct => new
            {
                ct.Id,
                ct.Amount,
                ct.BalanceAfter,
                ct.Type,
                ct.Description,
                ct.ReferenceId,
                ct.CreatedAt
            })
            .ToListAsync();

        return Ok(new { data = transactions, total, page, pageSize });
    }

    /// <summary>
    /// Add credits to client balance
    /// SECURITY (H-6): Restricted to Owner/Admin to prevent unauthorized credit grants.
    /// </summary>
    [HttpPost("client/{clientId}/add")]
    [Authorize(Roles = "Owner,Admin")]
    public async Task<IActionResult> AddCredit(Guid clientId, [FromBody] AddCreditRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        // SECURITY (M-5): Validate credit amount
        if (request.Amount <= 0)
            return BadRequest("Amount must be positive");
        if (request.Amount > 100_000)
            return BadRequest("Amount exceeds maximum allowed");

        // PAY-01 FIX: serialize per-client credit mutations (see DeductCredit). Keeps the recorded
        // BalanceAfter accurate and consistent when adds/deducts race for the same client.
        await using var tx = await _context.Database.BeginTransactionAsync();

        if (_context.Database.ProviderName != "Microsoft.EntityFrameworkCore.Sqlite")
        {
            await _context.Database.ExecuteSqlRawAsync(
                "SELECT 1 FROM \"Clients\" WHERE \"Id\" = {0} AND \"TenantId\" = {1} FOR UPDATE",
                clientId, tenantId.Value);
        }

        var client = await _context.Clients
            .FirstOrDefaultAsync(c => c.Id == clientId && c.TenantId == tenantId);

        if (client == null) return NotFound("Client not found");

        // Calculate current balance (inside the lock)
        var currentBalance = await _context.Set<CreditTransaction>()
            .Where(ct => ct.ClientId == clientId && ct.TenantId == tenantId)
            .SumAsync(ct => ct.Amount);

        var newBalance = currentBalance + request.Amount;

        var transaction = new CreditTransaction
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId.Value,
            ClientId = clientId,
            Amount = request.Amount,
            BalanceAfter = newBalance,
            Type = request.Type,
            Description = request.Description ?? GetDefaultDescription(request.Type, request.Amount),
            ReferenceId = request.ReferenceId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Set<CreditTransaction>().Add(transaction);
        await _context.SaveChangesAsync();
        await tx.CommitAsync();

        _logger.LogInformation("Added {Amount} credits to client {ClientId} ({Type})", request.Amount, clientId, request.Type);

        return Ok(new
        {
            success = true,
            transactionId = transaction.Id,
            newBalance
        });
    }

    /// <summary>
    /// Deduct credits from client balance
    /// SECURITY (H-6): Restricted to Owner/Admin to prevent unauthorized deductions.
    /// </summary>
    [HttpPost("client/{clientId}/deduct")]
    [Authorize(Roles = "Owner,Admin")]
    public async Task<IActionResult> DeductCredit(Guid clientId, [FromBody] DeductCreditRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        // SECURITY (M-5): Validate deduction amount
        if (request.Amount <= 0)
            return BadRequest("Amount must be positive");

        // PAY-01 FIX: serialize concurrent credit mutations for this client. Without this, the
        // read-balance → check → insert sequence is a check-then-act race: two concurrent deducts
        // both read the same balance, both pass the check, and both insert — overdrawing the
        // account (double-spend / negative balance). A per-client row lock (FOR UPDATE) forces
        // concurrent add/deduct for the SAME client to queue. SQLite (tests) relies on the
        // transaction alone.
        await using var tx = await _context.Database.BeginTransactionAsync();

        if (_context.Database.ProviderName != "Microsoft.EntityFrameworkCore.Sqlite")
        {
            await _context.Database.ExecuteSqlRawAsync(
                "SELECT 1 FROM \"Clients\" WHERE \"Id\" = {0} AND \"TenantId\" = {1} FOR UPDATE",
                clientId, tenantId.Value);
        }

        var client = await _context.Clients
            .FirstOrDefaultAsync(c => c.Id == clientId && c.TenantId == tenantId);

        if (client == null) return NotFound("Client not found");

        // Calculate current balance (inside the lock — no concurrent deduct can interleave)
        var currentBalance = await _context.Set<CreditTransaction>()
            .Where(ct => ct.ClientId == clientId && ct.TenantId == tenantId)
            .SumAsync(ct => ct.Amount);

        if (currentBalance < request.Amount)
            return BadRequest($"Insufficient credit balance. Current: {currentBalance:C}");

        var newBalance = currentBalance - request.Amount;

        var transaction = new CreditTransaction
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId.Value,
            ClientId = clientId,
            Amount = -request.Amount, // Negative for deduction
            BalanceAfter = newBalance,
            Type = request.Type,
            Description = request.Description ?? GetDefaultDescription(request.Type, -request.Amount),
            ReferenceId = request.ReferenceId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Set<CreditTransaction>().Add(transaction);
        await _context.SaveChangesAsync();
        await tx.CommitAsync();

        _logger.LogInformation("Deducted {Amount} credits from client {ClientId} ({Type})", request.Amount, clientId, request.Type);

        return Ok(new
        {
            success = true,
            transactionId = transaction.Id,
            newBalance
        });
    }

    /// <summary>
    /// Get all clients with credit balances
    /// </summary>
    [HttpGet("clients-with-balance")]
    public async Task<IActionResult> GetClientsWithBalance()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var clientsWithBalance = await _context.Set<CreditTransaction>()
            .Where(ct => ct.TenantId == tenantId)
            .GroupBy(ct => ct.ClientId)
            .Select(g => new
            {
                ClientId = g.Key,
                Balance = g.Sum(ct => ct.Amount)
            })
            .Where(x => x.Balance > 0)
            .ToListAsync();

        // Get client details
        var clientIds = clientsWithBalance.Select(x => x.ClientId).ToList();
        var clients = await _context.Clients
            .Where(c => clientIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c);

        var result = clientsWithBalance.Select(cb => new
        {
            cb.ClientId,
            cb.Balance,
            ClientName = clients.TryGetValue(cb.ClientId, out var c) ? c.FullName : "Unknown",
            Email = clients.TryGetValue(cb.ClientId, out var c2) ? c2.Email : null
        }).OrderByDescending(x => x.Balance).ToList();

        return Ok(new { data = result });
    }

    private string GetDefaultDescription(CreditTransactionType type, decimal amount)
    {
        return type switch
        {
            CreditTransactionType.Purchase => $"Credit purchase of {Math.Abs(amount):C}",
            CreditTransactionType.GiftCard => "Gift card redemption",
            CreditTransactionType.Refund => "Refund issued as credit",
            CreditTransactionType.Booking => "Applied to booking",
            CreditTransactionType.Adjustment => "Manual adjustment",
            CreditTransactionType.Expiry => "Credits expired",
            CreditTransactionType.Bonus => "Promotional bonus credits",
            _ => "Credit transaction"
        };
    }
}

// DTOs
public record AddCreditRequest(decimal Amount, CreditTransactionType Type = CreditTransactionType.Adjustment, string? Description = null, string? ReferenceId = null);
public record DeductCreditRequest(decimal Amount, CreditTransactionType Type = CreditTransactionType.Booking, string? Description = null, string? ReferenceId = null);

