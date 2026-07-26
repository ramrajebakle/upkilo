namespace Upkilo.Core.Entities;

/// <summary>
/// Inventory item entity - tracks products, supplies, and stock levels
/// </summary>
public class InventoryItem : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public Guid? ProductId { get; set; } // Link to Product entity
    public Guid? LocationId { get; set; } // Physical location
    public DateTime? LastRestockedAt { get; set; }
    public decimal CostPrice { get; set; }
    public decimal? SalePrice { get; set; } // If retail
    public int QuantityOnHand { get; set; }
    public int Quantity { get => QuantityOnHand; set => QuantityOnHand = value; } // Alias
    public int ReorderLevel { get; set; } = 5;
    public int LowStockThreshold { get => ReorderLevel; set => ReorderLevel = value; } // Alias
    public int? ReorderQuantity { get; set; }
    public string? Supplier { get; set; }
    public string? SupplierSku { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsRetail { get; set; } // Available for client purchase
    public DateTime? LastAlertSentAt { get; set; }

    // Navigation
    public virtual ICollection<InventoryTransaction> Transactions { get; set; } = new List<InventoryTransaction>();
}

/// <summary>
/// Inventory transaction entity - tracks stock movements
/// </summary>
public class InventoryTransaction : TenantEntity
{
    public Guid InventoryItemId { get; set; }
    public InventoryTransactionType Type { get; set; }
    public int Quantity { get; set; } // Positive for in, negative for out
    public int QuantityAfter { get; set; }
    public string? Notes { get; set; }
    public string? ReferenceType { get; set; } // Booking, Order, Adjustment
    public Guid? ReferenceId { get; set; }
    public Guid? UserId { get; set; }

    // Navigation
    public virtual InventoryItem? InventoryItem { get; set; }
}

public enum InventoryTransactionType
{
    StockIn,        // Received from supplier
    StockOut,       // Used during service
    Sale,           // Sold to client
    Adjustment,     // Manual correction
    Return,         // Returned to supplier
    Damaged,        // Marked as damaged
    Transfer        // Moved between locations
}
