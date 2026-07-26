namespace Upkilo.Core.Interfaces;

public interface ITriggerDispatcher
{
    Task DispatchAsync(string eventName, object data, Guid tenantId);
}
