using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

public class OperationsService : IOperationsService
{
    private readonly AppDbContext _context;
    private readonly ILogger<OperationsService> _logger;

    public OperationsService(AppDbContext context, ILogger<OperationsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ═══════════════════════════════════════════════════════
    // TENANT QUOTAS
    // ═══════════════════════════════════════════════════════
    public async Task<TenantQuota> GetQuotasAsync(Guid tenantId)
    {
        var quota = await _context.TenantQuotas.FirstOrDefaultAsync(q => q.TenantId == tenantId);
        if (quota == null)
        {
            quota = new TenantQuota { Id = Guid.NewGuid(), TenantId = tenantId };
            _context.TenantQuotas.Add(quota);
            await _context.SaveChangesAsync();
        }
        return quota;
    }

    public async Task<bool> UpdateQuotaAsync(Guid tenantId, string resource, int newLimit)
    {
        var quota = await GetQuotasAsync(tenantId);
        switch (resource.ToLower())
        {
            case "api": quota.MaxApiRequestsPerMinute = newLimit; break;
            case "storage": quota.MaxStorageMb = newLimit; break;
            case "staff": quota.MaxStaffMembers = newLimit; break;
            case "clients": quota.MaxClients = newLimit; break;
            case "emails": quota.MaxMonthlyEmails = newLimit; break;
            case "sms": quota.MaxMonthlySms = newLimit; break;
            case "ai_tokens": quota.MaxMonthlyAiTokens = newLimit; break;
            default: return false;
        }
        await _context.SaveChangesAsync();
        _logger.LogInformation("Quota {Resource} updated to {Limit} for tenant {Tenant}", resource, newLimit, tenantId);
        return true;
    }

    public async Task<bool> IncrementUsageAsync(Guid tenantId, string resource, int amount = 1)
    {
        var quota = await GetQuotasAsync(tenantId);

        // Reset if past the reset date
        if (DateTime.UtcNow > quota.QuotaResetDate)
        {
            quota.CurrentMonthlyEmails = 0;
            quota.CurrentMonthlySms = 0;
            quota.CurrentMonthlyAiTokens = 0;
            quota.QuotaResetDate = DateTime.UtcNow.AddMonths(1);
        }

        switch (resource.ToLower())
        {
            case "emails": quota.CurrentMonthlyEmails += amount; break;
            case "sms": quota.CurrentMonthlySms += amount; break;
            case "ai_tokens": quota.CurrentMonthlyAiTokens += amount; break;
            case "storage": quota.CurrentStorageUsedMb += amount; break;
            default: return false;
        }
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CheckQuotaAsync(Guid tenantId, string resource)
    {
        var quota = await GetQuotasAsync(tenantId);
        return resource.ToLower() switch
        {
            "emails" => quota.CurrentMonthlyEmails < quota.MaxMonthlyEmails,
            "sms" => quota.CurrentMonthlySms < quota.MaxMonthlySms,
            "ai_tokens" => quota.CurrentMonthlyAiTokens < quota.MaxMonthlyAiTokens,
            "storage" => quota.CurrentStorageUsedMb < quota.MaxStorageMb,
            _ => true
        };
    }

    // ═══════════════════════════════════════════════════════
    // WEBHOOK TRACKING
    // ═══════════════════════════════════════════════════════
    public async Task<WebhookDeliveryLog> LogWebhookAsync(Guid tenantId, string webhookType, string eventType, string payload, string? idempotencyKey)
    {
        // Check idempotency
        if (idempotencyKey != null)
        {
            var existing = await _context.WebhookDeliveryLogs
                .FirstOrDefaultAsync(w => w.IdempotencyKey == idempotencyKey && w.TenantId == tenantId);
            if (existing != null)
            {
                _logger.LogWarning("Duplicate webhook detected: {Key}", idempotencyKey);
                return existing;
            }
        }

        var log = new WebhookDeliveryLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            WebhookType = webhookType,
            EventType = eventType,
            Payload = payload,
            IdempotencyKey = idempotencyKey,
            Status = "Received"
        };

        _context.WebhookDeliveryLogs.Add(log);
        await _context.SaveChangesAsync();
        return log;
    }

    public async Task<IEnumerable<WebhookDeliveryLog>> GetWebhookLogsAsync(Guid tenantId, int count = 50)
    {
        return await _context.WebhookDeliveryLogs
            .Where(w => w.TenantId == tenantId)
            .OrderByDescending(w => w.ReceivedAt)
            .Take(count)
            .ToListAsync();
    }

    // ═══════════════════════════════════════════════════════
    // ADMIN IMPERSONATION
    // ═══════════════════════════════════════════════════════
    public async Task<AdminImpersonationLog> StartImpersonationAsync(Guid adminUserId, Guid targetTenantId, string reason, string ipAddress)
    {
        var session = new AdminImpersonationLog
        {
            AdminUserId = adminUserId,
            TargetTenantId = targetTenantId,
            Reason = reason,
            IpAddress = ipAddress
        };

        _context.AdminImpersonationLogs.Add(session);
        await _context.SaveChangesAsync();
        _logger.LogWarning("IMPERSONATION: Admin {Admin} started session for tenant {Tenant}, reason: {Reason}", adminUserId, targetTenantId, reason);
        return session;
    }

    public async Task<bool> EndImpersonationAsync(Guid sessionId, string? actionsPerformed)
    {
        var session = await _context.AdminImpersonationLogs.FindAsync(sessionId);
        if (session == null) return false;

        session.EndedAt = DateTime.UtcNow;
        session.ActionsPerformed = actionsPerformed;
        await _context.SaveChangesAsync();
        _logger.LogWarning("IMPERSONATION: Session {Id} ended", sessionId);
        return true;
    }

    // ═══════════════════════════════════════════════════════
    // STRIPE RECONCILIATION
    // ═══════════════════════════════════════════════════════
    public async Task<StripeReconciliationDto> ReconcileAsync(Guid tenantId, DateTime from, DateTime to)
    {
        var dbPayments = await _context.Payments
            .Where(p => p.TenantId == tenantId && p.CreatedAt >= from && p.CreatedAt <= to)
            .ToListAsync();

        var dbTotal = dbPayments.Sum(p => p.Amount);

        // In production, this would call Stripe API to cross-check
        var result = new StripeReconciliationDto
        {
            TotalPaymentsInDb = dbPayments.Count,
            TotalPaymentsInStripe = dbPayments.Count, // Would fetch from Stripe
            Matched = dbPayments.Count,
            Mismatched = 0,
            DbTotal = dbTotal,
            StripeTotal = dbTotal,
            Discrepancy = 0
        };

        _logger.LogInformation("Stripe reconciliation for tenant {Tenant}: {Count} payments, ${Total}", tenantId, result.Matched, result.DbTotal);
        return result;
    }
}
