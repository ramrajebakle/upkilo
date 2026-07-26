using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Upkilo.Core.Entities;

namespace Upkilo.Core.Interfaces;

public interface IInvoiceService
{
    Task<IEnumerable<Invoice>> GetInvoicesAsync(Guid tenantId, int page = 1, int pageSize = 10);
    Task<IEnumerable<Invoice>> GetClientInvoicesAsync(Guid tenantId, Guid clientId, int page = 1, int pageSize = 10);
    Task<Invoice?> GetInvoiceByIdAsync(Guid id, Guid tenantId);
    Task<Invoice> CreateInvoiceAsync(Invoice invoice);
    Task<byte[]> GenerateInvoicePdfAsync(Guid invoiceId, Guid tenantId);
    Task<string> GenerateInvoiceNumberAsync(Guid tenantId);
    Task<byte[]> GenerateThermalReceiptPdfAsync(Guid invoiceId, Guid tenantId);
    Task SendInvoiceByEmailAsync(Guid invoiceId, Guid tenantId);
    Task SendPaymentReceiptAsync(Guid invoiceId, Guid tenantId);
    Task UpdateInvoiceSettingsAsync(Guid tenantId, string prefix, long nextNumber);
    Task HandlePaymentFailureAsync(string stripeInvoiceId, string reason, long attemptCount, DateTime? nextPaymentAttempt);
    Task HandleDisputeAsync(string stripeChargeId, decimal amount, string reason);
    
    // Stripe Sync
    Task SyncStripeInvoiceAsync(string stripeInvoiceId);

    // Refunds & Pricing
    Task ProcessPartialRefundAsync(Guid invoiceId, decimal amount, string reason);
    Task<decimal> CalculateProrationAsync(Guid tenantId, decimal newPrice, DateTime effectiveDate);
}
