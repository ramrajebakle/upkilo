using System;

namespace Upkilo.Core.Entities;

public class InvoiceItem : BaseEntity
{
    public Guid InvoiceId { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; } // Quantity * UnitPrice
    
    public decimal TaxRate { get; set; } // e.g. 0.10 for 10%
    public decimal TaxAmount { get; set; } // Amount * TaxRate
    public decimal TotalAmount { get; set; } // Amount + TaxAmount
    public decimal Total { get => TotalAmount; set => TotalAmount = value; } // Alias
    public Guid TenantId { get; set; }

    public virtual Invoice? Invoice { get; set; }
}
