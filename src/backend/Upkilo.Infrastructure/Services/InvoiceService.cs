using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Stripe;
using Upkilo.Core.Entities;
using Upkilo.Core.Helpers;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;
using Upkilo.Infrastructure.Interfaces;
using Upkilo.Infrastructure.Helpers;

namespace Upkilo.Infrastructure.Services;

public class InvoiceService : IInvoiceService
{
    private readonly AppDbContext _context;
    private readonly ILogger<InvoiceService> _logger;
    private readonly IEmailService _emailService;
    private readonly string _stripeApiKey;

    public InvoiceService(
        AppDbContext context,
        ILogger<InvoiceService> logger,
        IEmailService emailService,
        Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _emailService = emailService;
        _stripeApiKey = configuration["Stripe:SecretKey"] ?? "";
        
        // License for QuestPDF Community
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<IEnumerable<Upkilo.Core.Entities.Invoice>> GetInvoicesAsync(Guid tenantId, int page = 1, int pageSize = 10)
    {
        return await _context.Invoices
            .Where(i => i.TenantId == tenantId)
            .OrderByDescending(i => i.IssueDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<Upkilo.Core.Entities.Invoice>> GetClientInvoicesAsync(Guid tenantId, Guid clientId, int page = 1, int pageSize = 10)
    {
        return await _context.Invoices
            .Where(i => i.TenantId == tenantId && i.ClientId == clientId)
            .OrderByDescending(i => i.IssueDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<Upkilo.Core.Entities.Invoice?> GetInvoiceByIdAsync(Guid id, Guid tenantId)
    {
        return await _context.Invoices
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == id && i.TenantId == tenantId);
    }

    public async Task<Upkilo.Core.Entities.Invoice> CreateInvoiceAsync(Upkilo.Core.Entities.Invoice invoice)
    {
        if (string.IsNullOrEmpty(invoice.InvoiceNumber))
        {
            invoice.InvoiceNumber = await GenerateInvoiceNumberAsync(invoice.TenantId);
        }

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();
        return invoice;
    }

    public async Task<string> GenerateInvoiceNumberAsync(Guid tenantId)
    {
        var tenant = await _context.Tenants.FindAsync(tenantId);
        if (tenant == null) throw new KeyNotFoundException("Tenant not found");

        var prefix = "INV-";
        if (tenant.Settings.TryGetValue("InvoicePrefix", out var p) && p is string s)
        {
            prefix = s;
        }

        long nextNumber = 1000;
        if (tenant.Settings.TryGetValue("NextInvoiceNumber", out var n))
        {
            if (n is long l) nextNumber = l;
            else if (n is int i) nextNumber = i;
            else if (n is string str && long.TryParse(str, out var parsed)) nextNumber = parsed;
            // Handle JsonElement if using System.Text.Json default deserialization
            else if (n is System.Text.Json.JsonElement je && je.ValueKind == System.Text.Json.JsonValueKind.Number) nextNumber = je.GetInt64();
        }

        var invoiceNumber = $"{prefix}{nextNumber}";

        // Increment and save
        tenant.Settings["NextInvoiceNumber"] = nextNumber + 1;
        
        // Ensure "InvoicePrefix" is saved if it wasn't there? Optional, but good for consistency.
        if (!tenant.Settings.ContainsKey("InvoicePrefix")) tenant.Settings["InvoicePrefix"] = prefix;

        // Mark as modified if using EF Core with dictionary change tracking issues
        _context.Entry(tenant).State = EntityState.Modified; 
        
        // We'll save changes here to reserve the number. 
        // Note: In high concurrency, this simple approach might have race conditions, but for this scale it's acceptable.
        // A better approach would be a dedicated sequence table or atomic atomic update, 
        // but since we are storing in a JSON blob in the Tenant entity, we rely on EF Core optimistic concurrency if enabled, or last-writer-wins.
        await _context.SaveChangesAsync();

        return invoiceNumber;
    }

    public async Task SyncStripeInvoiceAsync(string stripeInvoiceId)
    {
        try
        {
            // We need to instantiate Stripe's service explicitly.
            var stripeService = new Stripe.InvoiceService(); 
            var stripeInvoice = await stripeService.GetAsync(stripeInvoiceId);

            if (stripeInvoice == null || !stripeInvoice.Metadata.ContainsKey("tenant_id"))
            {
                _logger.LogWarning("Stripe invoice {Id} ignored (no tenant_id)", stripeInvoiceId);
                return;
            }

            var tenantId = Guid.Parse(stripeInvoice.Metadata["tenant_id"]);
            
            // Check if exists
            var existing = await _context.Invoices.FirstOrDefaultAsync(i => i.StripeInvoiceId == stripeInvoiceId);
            if (existing != null)
            {
                existing.Status = MapStatus(stripeInvoice.Status);
                existing.PdfUrl = stripeInvoice.InvoicePdf;
                existing.HostedInvoiceUrl = stripeInvoice.HostedInvoiceUrl;
                if (stripeInvoice.Status == "paid" && existing.PaidAt == null)
                {
                    existing.PaidAt = DateTime.UtcNow;
                }
                await _context.SaveChangesAsync();
                return;
            }

            // Create new
            var newInvoice = new Upkilo.Core.Entities.Invoice
            {
                TenantId = tenantId,
                StripeInvoiceId = stripeInvoice.Id,
                InvoiceNumber = stripeInvoice.Number,
                IssueDate = stripeInvoice.Created,
                DueDate = stripeInvoice.DueDate ?? stripeInvoice.Created,
                Status = MapStatus(stripeInvoice.Status),
                TotalAmount = Currency.FromMinorUnits(stripeInvoice.Total, stripeInvoice.Currency),
                // Normalized to uppercase: Stripe returns lowercase codes, and StripeWebhookController
                // stores them uppercased. Persisting both spellings made currency comparisons
                // between the two write paths miss.
                Currency = Currency.Normalize(stripeInvoice.Currency),
                CustomerName = stripeInvoice.CustomerName ?? "Customer",
                CustomerEmail = stripeInvoice.CustomerEmail,
                PdfUrl = stripeInvoice.InvoicePdf,
                HostedInvoiceUrl = stripeInvoice.HostedInvoiceUrl,
                PaidAt = stripeInvoice.Status == "paid" ? (DateTime?)DateTime.UtcNow : null
            };
            
            // Add lines
            // Add lines
            foreach(var line in stripeInvoice.Lines.Data)
            {
                var quantity = (int)(line.Quantity ?? 1);
                var totalAmount = (decimal)line.Amount;
                var unitPrice = quantity > 0 ? totalAmount / quantity : totalAmount;
                
                // Process Stripe Taxes if available
                var stripeTax = line.Taxes?.Sum(t => t.Amount) ?? 0;
                var taxRate = (stripeTax > 0 && totalAmount > 0) ? (decimal)stripeTax / (decimal)totalAmount : 0m;

                // Fallback to Tenant default tax rate if Stripe didn't compute it
                if (stripeTax == 0)
                {
                    var tenant = await _context.Tenants.FindAsync(tenantId);
                    if (tenant != null && tenant.Settings.TryGetValue("DefaultTaxRate", out var defaultTaxObj))
                    {
                        if (decimal.TryParse(defaultTaxObj?.ToString(), out var tenantTaxRate))
                        {
                            taxRate = tenantTaxRate;
                            stripeTax = (long)(totalAmount * taxRate);
                        }
                    }
                }

                newInvoice.Items.Add(new Upkilo.Core.Entities.InvoiceItem
                {
                    Description = line.Description ?? "Item",
                    Quantity = quantity,
                    UnitPrice = Currency.FromMinorUnits((long)unitPrice, stripeInvoice.Currency),
                    Amount = Currency.FromMinorUnits((long)totalAmount, stripeInvoice.Currency),
                    TaxAmount = Currency.FromMinorUnits(stripeTax, stripeInvoice.Currency),
                    TaxRate = taxRate,
                    TotalAmount = Currency.FromMinorUnits((long)totalAmount + stripeTax, stripeInvoice.Currency)
                });
            }

            _context.Invoices.Add(newInvoice);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync Stripe invoice {Id}", stripeInvoiceId);
        }
    }

    private InvoiceStatus MapStatus(string stripeStatus)
    {
        return stripeStatus switch
        {
            "paid" => InvoiceStatus.Paid,
            "open" => InvoiceStatus.Sent,
            "draft" => InvoiceStatus.Draft,
            "void" => InvoiceStatus.Void,
            "uncollectible" => InvoiceStatus.Uncollectible,
            _ => InvoiceStatus.Draft
        };
    }

    public async Task<byte[]> GenerateInvoicePdfAsync(Guid invoiceId, Guid tenantId)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == invoiceId && i.TenantId == tenantId);
        
        if (invoice == null) throw new KeyNotFoundException("Invoice not found");

        var tenant = await _context.Tenants.FindAsync(tenantId);
        if (tenant == null) throw new KeyNotFoundException("Tenant not found");

        // Simple template factory
        IInvoiceTemplate template = new Templates.ModernInvoiceTemplate();

        var document = Document.Create(container =>
        {
            template.Compose(container, invoice, tenant, tenant.Settings);
        });

        return document.GeneratePdf();
    }

    public async Task<byte[]> GenerateThermalReceiptPdfAsync(Guid invoiceId, Guid tenantId)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == invoiceId && i.TenantId == tenantId);
        
        if (invoice == null) throw new KeyNotFoundException("Invoice not found");

        var tenant = await _context.Tenants.FindAsync(tenantId);
        if (tenant == null) throw new KeyNotFoundException("Tenant not found");

        IInvoiceTemplate template = new Templates.ThermalReceiptTemplate();

        var document = Document.Create(container =>
        {
            template.Compose(container, invoice, tenant, tenant.Settings);
        });

        return document.GeneratePdf();
    }

    public async Task SendInvoiceByEmailAsync(Guid invoiceId, Guid tenantId)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == invoiceId && i.TenantId == tenantId);
            
        if (invoice == null) throw new KeyNotFoundException("Invoice not found");

        var tenant = await _context.Tenants.FindAsync(tenantId);
        if (tenant == null) throw new KeyNotFoundException("Tenant not found");

        var pdfBytes = await GenerateInvoicePdfAsync(invoiceId, tenantId);
        
        var subject = $"{InvoiceTranslator.GetLabel("Invoice", tenant.Locale)} #{invoice.InvoiceNumber} - {tenant.Name}";
        var body = $@"
            <div style='font-family: sans-serif;'>
                <h2>{InvoiceTranslator.GetLabel("Invoice", tenant.Locale)} #{invoice.InvoiceNumber}</h2>
                <p>Hi {invoice.CustomerName},</p>
                <p>Please find attached your invoice from <strong>{tenant.Name}</strong>.</p>
                <p><strong>{InvoiceTranslator.GetLabel("Total", tenant.Locale)}:</strong> {Upkilo.Core.Helpers.Currency.Format(invoice.TotalAmount, invoice.Currency)}</p>
                <p>{InvoiceTranslator.GetLabel("ThankYou", tenant.Locale)}</p>
                <br/>
                <p>Best regards,<br/>{tenant.Name}</p>
            </div>";

        await _emailService.SendInvoiceAsync(new InvoiceEmailData(
            invoice.CustomerEmail ?? "",
            invoice.CustomerName,
            subject,
            body,
            pdfBytes,
            $"Invoice-{invoice.InvoiceNumber}.pdf"
        ));
    }

    public async Task SendPaymentReceiptAsync(Guid invoiceId, Guid tenantId)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == invoiceId && i.TenantId == tenantId);
            
        if (invoice == null) throw new KeyNotFoundException("Invoice not found");
        if (invoice.Status != InvoiceStatus.Paid) throw new InvalidOperationException("Cannot send receipt for unpaid invoice");

        var tenant = await _context.Tenants.FindAsync(tenantId);
        if (tenant == null) throw new KeyNotFoundException("Tenant not found");

        var pdfBytes = await GenerateInvoicePdfAsync(invoiceId, tenantId);
        
        var subject = $"{InvoiceTranslator.GetLabel("Receipt", tenant.Locale)} for Invoice #{invoice.InvoiceNumber} - {tenant.Name}";
        var body = $@"
            <div style='font-family: sans-serif;'>
                <h2 style='color: #059669;'>{InvoiceTranslator.GetLabel("Paid", tenant.Locale)}</h2>
                <p>Hi {invoice.CustomerName},</p>
                <p>This is a receipt for your recent payment to <strong>{tenant.Name}</strong>.</p>
                <p><strong>{InvoiceTranslator.GetLabel("Total", tenant.Locale)}:</strong> {Upkilo.Core.Helpers.Currency.Format(invoice.TotalAmount, invoice.Currency)}</p>
                <p>{InvoiceTranslator.GetLabel("ThankYou", tenant.Locale)}</p>
                <br/>
                <p>Best regards,<br/>{tenant.Name}</p>
            </div>";

        await _emailService.SendPaymentReceiptAsync(new InvoiceEmailData(
            invoice.CustomerEmail ?? "",
            invoice.CustomerName,
            subject,
            body,
            pdfBytes,
            $"Receipt-{invoice.InvoiceNumber}.pdf"
        ));
        await _emailService.SendPaymentReceiptAsync(new InvoiceEmailData(
            invoice.CustomerEmail ?? "",
            invoice.CustomerName,
            subject,
            body,
            pdfBytes,
            $"Receipt-{invoice.InvoiceNumber}.pdf"
        ));
    }

    public async Task UpdateInvoiceSettingsAsync(Guid tenantId, string prefix, long nextNumber)
    {
        var tenant = await _context.Tenants.FindAsync(tenantId);
        if (tenant == null) throw new KeyNotFoundException("Tenant not found");

        tenant.Settings["InvoicePrefix"] = prefix;
        tenant.Settings["NextInvoiceNumber"] = nextNumber;

        _context.Entry(tenant).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        _context.Entry(tenant).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }

    public async Task HandlePaymentFailureAsync(string stripeInvoiceId, string reason, long attemptCount, DateTime? nextPaymentAttempt)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.StripeInvoiceId == stripeInvoiceId);

        if (invoice == null)
        {
            _logger.LogWarning("Invoice check failed: Stripe invoice {Id} not found", stripeInvoiceId);
            return;
        }

        var tenant = await _context.Tenants.FindAsync(invoice.TenantId);
        if (tenant == null) return;

        // Update status
        invoice.Status = InvoiceStatus.Overdue;
        invoice.Metadata["FailureReason"] = reason;
        invoice.Metadata["DunningAttempt"] = attemptCount;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Marked invoice {InvoiceNumber} as Overdue (attempt {Attempt})", invoice.InvoiceNumber, attemptCount);

        // Escalating email logic
        string subject;
        string urgencyText;
        string headerColor;

        if (attemptCount == 1)
        {
            subject = $"Payment Failed for Invoice #{invoice.InvoiceNumber}";
            urgencyText = "We noticed an issue with your payment. Please update your card details at your convenience.";
            headerColor = "#F59E0B"; // Amber - gentle
        }
        else if (attemptCount == 2)
        {
            subject = $"URGENT: Payment Still Failing for Invoice #{invoice.InvoiceNumber}";
            urgencyText = "Your payment has failed again. Please update your payment method immediately to avoid service interruption.";
            headerColor = "#EF4444"; // Red - urgent
        }
        else
        {
            subject = $"FINAL NOTICE: Invoice #{invoice.InvoiceNumber} - Service Suspension Imminent";
            urgencyText = "This is your final notice. Your service may be suspended if payment is not received soon.";
            headerColor = "#DC2626"; // Dark Red - critical
        }

        var nextAttemptText = nextPaymentAttempt.HasValue
            ? $"<p><strong>Next automatic attempt:</strong> {nextPaymentAttempt.Value:g} UTC</p>"
            : "";

        var body = $@"
            <div style='font-family: sans-serif;'>
                <h2 style='color: {headerColor};'>Payment Failed (Attempt {attemptCount})</h2>
                <p>Hi {invoice.CustomerName},</p>
                <p>{urgencyText}</p>
                <p><strong>Invoice:</strong> #{invoice.InvoiceNumber}</p>
                <p><strong>Reason:</strong> {reason}</p>
                {nextAttemptText}
                <p><a href='{invoice.HostedInvoiceUrl}' style='display: inline-block; padding: 10px 20px; background-color: #06B6D4; color: white; text-decoration: none; border-radius: 5px;'>Pay Now</a></p>
                <br/>
                <p>Best regards,<br/>{tenant.Name}</p>
            </div>";

        await _emailService.SendPaymentFailureEmailAsync(new InvoiceEmailData(
            invoice.CustomerEmail ?? "",
            invoice.CustomerName,
            subject,
            body,
            Array.Empty<byte>(),
            "" 
        ));
    }

    public async Task HandleDisputeAsync(string stripeChargeId, decimal amount, string reason)
    {
        // Find payment by charge ID or intent ID (sometimes disputes come with charge ID)
        var payment = await _context.Payments
            .Include(p => p.Tenant)
            .Include(p => p.Client)
            .FirstOrDefaultAsync(p => p.StripeChargeId == stripeChargeId || p.StripePaymentIntentId == stripeChargeId);

        if (payment == null)
        {
            _logger.LogWarning("Dispute check failed: Payment with Charge/Intent ID {Id} not found", stripeChargeId);
            return;
        }

        // Update status
        payment.Status = PaymentStatus.Disputed;
        if (!payment.Metadata.ContainsKey("DisputeReason"))
        {
            payment.Metadata.Add("DisputeReason", reason);
        }
        else
        {
            payment.Metadata["DisputeReason"] = reason;
        }

        await _context.SaveChangesAsync();
        _logger.LogWarning("Payment {PaymentId} marked as Disputed", payment.Id);

        // Notify Tenant Owner
        if (payment.Tenant != null && !string.IsNullOrEmpty(payment.Tenant.Email))
        {
            await _emailService.SendDisputeAlertAsync(
                payment.Tenant.Email,
                payment.Tenant.Name,
                payment.Client?.FirstName + " " + payment.Client?.LastName ?? "Customer",
                amount,
                reason
            );
        }
    }

    public async Task ProcessPartialRefundAsync(Guid invoiceId, decimal amount, string reason)
    {
        var invoice = await _context.Invoices.FindAsync(invoiceId);
        if (invoice == null) throw new KeyNotFoundException("Invoice not found");

        if (invoice.PaidAt == null) throw new InvalidOperationException("Cannot refund an unpaid invoice");

        invoice.RefundedAmount += amount;
        invoice.RefundedAt = DateTime.UtcNow;
        
        if (invoice.RefundedAmount >= invoice.TotalAmount)
        {
            invoice.Status = InvoiceStatus.Refunded;
        }

        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Processed partial refund of {Amount} for invoice {InvoiceNumber}. Reason: {Reason}", 
            amount, invoice.InvoiceNumber, reason);
    }

    public async Task<decimal> CalculateProrationAsync(Guid tenantId, decimal newPrice, DateTime effectiveDate)
    {
        var tenant = await _context.Tenants.FindAsync(tenantId);
        if (tenant == null || !tenant.SubscriptionPeriodEnd.HasValue) return 0;

        var periodStart = tenant.SubscriptionPeriodEnd.Value.AddMonths(-1); // Assuming monthly
        var periodEnd = tenant.SubscriptionPeriodEnd.Value;
        
        if (effectiveDate < periodStart || effectiveDate > periodEnd) return 0;

        var totalDays = (periodEnd - periodStart).TotalDays;
        var remainingDays = (periodEnd - effectiveDate).TotalDays;

        if (totalDays <= 0) return 0;

        var prorationFactor = (decimal)(remainingDays / totalDays);
        var currentPrice = 0m; 

        // Fetch current price from the new DB architecture
        var currentPlan = await _context.PricingPlans
            .Include(p => p.Prices)
            .FirstOrDefaultAsync(p => p.Id == tenant.PricingPlanId);
            
        currentPrice = currentPlan?.Prices
            .Where(p => p.Cycle == BillingCycle.Monthly)
            .Select(p => p.Amount)
            .FirstOrDefault() ?? 0m;

        var unusedAmount = currentPrice * prorationFactor;
        var newAmount = newPrice * prorationFactor;

        return newAmount - unusedAmount;
    }
}
