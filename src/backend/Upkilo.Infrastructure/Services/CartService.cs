using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services
{
    public class CartService : ICartService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<CartService> _logger;

        public CartService(AppDbContext context, ILogger<CartService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<CartItem> AddToCartAsync(Guid tenantId, Guid? clientId, string sessionId, Guid productId, int quantity, string? variant = null)
        {
            _logger.LogInformation("Adding product {ProductId} to cart", productId);

            var product = await _context.Products.FindAsync(productId);
            if (product == null || !product.IsActive) throw new Exception("Product not found");

            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.TenantId == tenantId &&
                                          ((clientId.HasValue && c.ClientId == clientId) ||
                                           (!clientId.HasValue && c.SessionId == sessionId)) &&
                                          c.ProductId == productId &&
                                          c.SelectedVariant == variant);

            if (cartItem != null)
            {
                cartItem.Quantity += quantity;
            }
            else
            {
                cartItem = new CartItem
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    ClientId = clientId,
                    SessionId = sessionId,
                    ProductId = productId,
                    Quantity = quantity,
                    SelectedVariant = variant,
                    CreatedAt = DateTime.UtcNow
                };
                _context.CartItems.Add(cartItem);
            }

            // Simple inventory check if enabled
            if (product.TrackInventory && product.StockQuantity < cartItem.Quantity)
            {
                throw new Exception("Insufficient stock available");
            }

            await _context.SaveChangesAsync();
            return cartItem;
        }

        public async Task RemoveFromCartAsync(Guid tenantId, Guid? clientId, string sessionId, Guid productId)
        {
            var cartItems = await _context.CartItems
                .Where(c => c.TenantId == tenantId &&
                            ((clientId.HasValue && c.ClientId == clientId) ||
                             (!clientId.HasValue && c.SessionId == sessionId)) &&
                            c.ProductId == productId)
                .ToListAsync();

            if (cartItems.Any())
            {
                _context.CartItems.RemoveRange(cartItems);
                await _context.SaveChangesAsync();
            }
        }

        public async Task UpdateQuantityAsync(Guid tenantId, Guid? clientId, string sessionId, Guid productId, int quantity)
        {
            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.TenantId == tenantId &&
                                          ((clientId.HasValue && c.ClientId == clientId) ||
                                           (!clientId.HasValue && c.SessionId == sessionId)) &&
                                          c.ProductId == productId);

            if (cartItem != null)
            {
                if (quantity <= 0)
                {
                    _context.CartItems.Remove(cartItem);
                }
                else
                {
                    // Check stock
                    var product = await _context.Products.FindAsync(productId);
                    if (product != null && product.TrackInventory && product.StockQuantity < quantity)
                    {
                        throw new Exception("Insufficient stock");
                    }
                    cartItem.Quantity = quantity;
                }
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<CartItem>> GetCartAsync(Guid tenantId, Guid? clientId, string sessionId)
        {
            return await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.TenantId == tenantId &&
                            ((clientId.HasValue && c.ClientId == clientId) ||
                             (!clientId.HasValue && c.SessionId == sessionId)))
                .ToListAsync();
        }

        public async Task ClearCartAsync(Guid tenantId, Guid? clientId, string sessionId)
        {
            var cartItems = await _context.CartItems
                .Where(c => c.TenantId == tenantId &&
                            ((clientId.HasValue && c.ClientId == clientId) ||
                             (!clientId.HasValue && c.SessionId == sessionId)))
                .ToListAsync();

            _context.CartItems.RemoveRange(cartItems);
            await _context.SaveChangesAsync();
        }

        public async Task MergeCartAsync(Guid tenantId, string sessionId, Guid clientId)
        {
            _logger.LogInformation("Merging guest cart {SessionId} into client {ClientId}", sessionId, clientId);

            var guestItems = await _context.CartItems
                .Where(c => c.TenantId == tenantId && c.SessionId == sessionId && c.ClientId == null)
                .ToListAsync();

            foreach (var guestItem in guestItems)
            {
                // Try to find if client already has this product in cart
                var clientItem = await _context.CartItems
                    .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.ClientId == clientId &&
                                              c.ProductId == guestItem.ProductId &&
                                              c.SelectedVariant == guestItem.SelectedVariant);

                if (clientItem != null)
                {
                    clientItem.Quantity += guestItem.Quantity;
                    _context.CartItems.Remove(guestItem);
                }
                else
                {
                    guestItem.ClientId = clientId;
                    guestItem.SessionId = null; // Clear session link as it's now owned by user
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}
