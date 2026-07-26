namespace Upkilo.Core.Entities;

public class Supplier : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Website { get; set; }
    public string? PaymentTerms { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
}

public enum PurchaseOrderStatus
{
    Draft,
    Submitted,
    PartiallyReceived,
    Received,
    Cancelled
}

public class PurchaseOrder : TenantEntity
{
    public string OrderNumber { get; set; } = string.Empty;
    public Guid SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;
    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;
    public string? Notes { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public string? ItemsJson { get; set; } // JSON array of line items
}
