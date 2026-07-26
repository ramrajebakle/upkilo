using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Upkilo.Core.Interfaces;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Upkilo.API.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class ShopController : ControllerBase
    {
        private readonly ICartService _cartService;
        private readonly ITenantProvider _tenantProvider;
        private readonly AppDbContext _context;

        public ShopController(ICartService cartService, ITenantProvider tenantProvider, AppDbContext context)
        {
            _cartService = cartService;
            _tenantProvider = tenantProvider;
            _context = context;
        }

        private Guid GetTenantId() => _tenantProvider.GetTenantId()
            ?? throw new UnauthorizedAccessException("Tenant context not available");

        /// <summary>
        /// Get all active products for the shop
        /// </summary>
        [HttpGet("products")]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts([FromQuery] string? category = null)
        {
            var query = _context.Products.Where(p => p.TenantId == GetTenantId() && p.IsActive);
            // In a real system, product category would be a separate entity or field
            // if (!string.IsNullOrEmpty(category)) query = query.Where(p => p.Category == category);
            
            return await query.OrderBy(p => p.Name).ToListAsync();
        }

        /// <summary>
        /// Get current user's cart
        /// </summary>
        [HttpGet("cart")]
        public async Task<IActionResult> GetCart([FromQuery] string? sessionId)
        {
            var clientId = _tenantProvider.GetUserId(); // Assuming user ID maps to client in this context, or null for guests
            var cart = await _cartService.GetCartAsync(GetTenantId(), clientId == Guid.Empty ? null : clientId, sessionId ?? "");
            return Ok(cart);
        }

        /// <summary>
        /// Add item to cart
        /// </summary>
        [HttpPost("cart/add")]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request)
        {
            try
            {
                var clientId = _tenantProvider.GetUserId();
                var item = await _cartService.AddToCartAsync(
                    GetTenantId(),
                    clientId == Guid.Empty ? null : clientId,
                    request.SessionId,
                    request.ProductId,
                    request.Quantity,
                    request.Variant);
                return Ok(item);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Update item quantity in cart
        /// </summary>
        [HttpPut("cart/update")]
        public async Task<IActionResult> UpdateCart([FromBody] UpdateCartRequest request)
        {
            var clientId = _tenantProvider.GetUserId();
            await _cartService.UpdateQuantityAsync(
                GetTenantId(),
                clientId == Guid.Empty ? null : clientId,
                request.SessionId,
                request.ProductId,
                request.Quantity);
            return Ok();
        }

        /// <summary>
        /// Remove item from cart
        /// </summary>
        [HttpDelete("cart/remove")]
        public async Task<IActionResult> RemoveFromCart([FromQuery] Guid productId, [FromQuery] string sessionId)
        {
            var clientId = _tenantProvider.GetUserId();
            await _cartService.RemoveFromCartAsync(GetTenantId(), clientId == Guid.Empty ? null : clientId, sessionId, productId);
            return Ok();
        }

        /// <summary>
        /// Merge guest cart (called on login)
        /// </summary>
        [HttpPost("cart/merge")]
        [Authorize]
        public async Task<IActionResult> MergeCart([FromQuery] string sessionId)
        {
            var clientId = _tenantProvider.GetUserId();
            if (clientId == null || clientId == Guid.Empty) return Unauthorized();
            
            await _cartService.MergeCartAsync(GetTenantId(), sessionId, clientId.Value);
            return Ok(new { message = "Cart merged" });
        }
    }

    public class AddToCartRequest
    {
        public string SessionId { get; set; } = string.Empty;
        public Guid ProductId { get; set; }
        public int Quantity { get; set; } = 1;
        public string? Variant { get; set; }
    }

    public class UpdateCartRequest
    {
        public string SessionId { get; set; } = string.Empty;
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
