using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Upkilo.Infrastructure.Data;
using Upkilo.Core.Events;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Upkilo.Infrastructure.Events;

public class InventoryDecrementHandler : INotificationHandler<OrderCompletedEvent>
{
    private readonly AppDbContext _context;
    private readonly ILogger<InventoryDecrementHandler> _logger;

    public InventoryDecrementHandler(AppDbContext context, ILogger<InventoryDecrementHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Handle(OrderCompletedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing OrderCompletedEvent {OrderId} to decrement inventory", notification.OrderId);

        foreach (var item in notification.Items)
        {
            var inventory = await _context.InventoryItems
                .Where(i => i.TenantId == notification.TenantId && i.ProductId == item.ProductId)
                .OrderByDescending(i => i.Quantity) // Pick location with most stock (simplified)
                .FirstOrDefaultAsync(cancellationToken);

            if (inventory != null)
            {
                inventory.Quantity -= item.Quantity;
                if (inventory.Quantity < 0) inventory.Quantity = 0;

                // Fire LowStock Alert if threshold met
                if (inventory.Quantity <= inventory.LowStockThreshold)
                {
                    _logger.LogWarning("Low stock alert for Product {ProductId}. Remaining: {Qty}", item.ProductId, inventory.Quantity);
                    // System logic: Publish LowStockEvent
                }
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
