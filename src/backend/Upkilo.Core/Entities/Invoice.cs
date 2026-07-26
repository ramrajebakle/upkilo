using System;
using System.Collections.Generic;

namespace Upkilo.Core.Entities;

public class Invoice : TenantEntity
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public DateTime IssuedAt { get => IssueDate; set => IssueDate = value; }
    public DateTime DueDate { get; set; }
    public Guid? ClientId { get; set; }
    public string? Type { get; set; } // Service, Subscription, Product
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "USD";
    
    // Customer Details
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public string? BillToAddress { get; set; }
    
    // Stripe Integration
    public string? StripeInvoiceId { get; set; }
    public string? SubscriptionId { get; set; }
    public string? HostedInvoiceUrl { get; set; }
    public string? PdfUrl { get; set; } 
    public string? Industry { get; set; } 

    // Internal Billing Fields
    public DateTime? PaidAt { get; set; }
    public decimal RefundedAmount { get; set; }
    public DateTime? RefundedAt { get; set; }

    public Dictionary<string, object> Metadata { get; set; } = new();

    public virtual ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
}

public enum InvoiceStatus
{
    Draft,
    Sent,
    Paid,
    Refunded,
    Void,
    Overdue,
    Uncollectible
}
