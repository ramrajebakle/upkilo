using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Upkilo.Core.Entities;

namespace Upkilo.Core.Interfaces;

public interface IOperationsService
{
    // Tenant Quotas
    Task<TenantQuota> GetQuotasAsync(Guid tenantId);
    Task<bool> UpdateQuotaAsync(Guid tenantId, string resource, int newLimit);
    Task<bool> IncrementUsageAsync(Guid tenantId, string resource, int amount = 1);
    Task<bool> CheckQuotaAsync(Guid tenantId, string resource);

    // Webhook Tracking
    Task<WebhookDeliveryLog> LogWebhookAsync(Guid tenantId, string webhookType, string eventType, string payload, string? idempotencyKey);
    Task<IEnumerable<WebhookDeliveryLog>> GetWebhookLogsAsync(Guid tenantId, int count = 50);

    // Admin Impersonation
    Task<AdminImpersonationLog> StartImpersonationAsync(Guid adminUserId, Guid targetTenantId, string reason, string ipAddress);
    Task<bool> EndImpersonationAsync(Guid sessionId, string? actionsPerformed);

    // Stripe Reconciliation
    Task<StripeReconciliationDto> ReconcileAsync(Guid tenantId, DateTime from, DateTime to);
}

public class StripeReconciliationDto
{
    public int TotalPaymentsInDb { get; set; }
    public int TotalPaymentsInStripe { get; set; }
    public int Matched { get; set; }
    public int Mismatched { get; set; }
    public decimal DbTotal { get; set; }
    public decimal StripeTotal { get; set; }
    public decimal Discrepancy { get; set; }
    public List<string> Issues { get; set; } = new();
}
