using Upkilo.Core.Entities;

namespace Upkilo.Core.Interfaces
{
    public interface IEventService
    {
        Task PublishAsync(string eventName, object data, Guid tenantId);
    }
}
