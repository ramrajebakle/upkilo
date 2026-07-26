namespace Upkilo.Core.Interfaces;

public interface ISystemLoadMonitorService
{
    bool IsSystemDegraded();
    bool IsSystemOverloaded();
}
