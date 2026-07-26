using System.Threading.Channels;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;

namespace Upkilo.Infrastructure.Services
{
    public class EventService : IEventService
    {
        private readonly Channel<WorkflowEvent> _channel;

        public EventService()
        {
            // Bounded channel to ensure system stability and backpressure
            var options = new BoundedChannelOptions(1000)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true, // Usually consumed by a single background worker
                SingleWriter = false
            };
            _channel = Channel.CreateBounded<WorkflowEvent>(options);
        }

        public async Task PublishAsync(string eventName, object data, Guid tenantId)
        {
            var evt = new WorkflowEvent
            {
                EventName = eventName,
                Data = data,
                TenantId = tenantId,
                OccurredAt = DateTime.UtcNow
            };

            await _channel.Writer.WriteAsync(evt);
        }

        public ChannelReader<WorkflowEvent> Reader => _channel.Reader;
    }
}
