using MediatR;

namespace Upkilo.Core.Events;

public class OrderCompletedEvent : INotification
{
    public Guid OrderId { get; set; }
    public Guid TenantId { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();

    public class OrderItemDto
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
