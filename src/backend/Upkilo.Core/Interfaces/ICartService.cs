using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Upkilo.Core.Entities;

namespace Upkilo.Core.Interfaces
{
    public interface ICartService
    {
        Task<CartItem> AddToCartAsync(Guid tenantId, Guid? clientId, string sessionId, Guid productId, int quantity, string? variant = null);
        Task RemoveFromCartAsync(Guid tenantId, Guid? clientId, string sessionId, Guid productId);
        Task UpdateQuantityAsync(Guid tenantId, Guid? clientId, string sessionId, Guid productId, int quantity);
        Task<IEnumerable<CartItem>> GetCartAsync(Guid tenantId, Guid? clientId, string sessionId);
        Task ClearCartAsync(Guid tenantId, Guid? clientId, string sessionId);
        Task MergeCartAsync(Guid tenantId, string sessionId, Guid clientId);
    }
}
