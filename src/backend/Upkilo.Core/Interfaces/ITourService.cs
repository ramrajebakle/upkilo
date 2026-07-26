using Upkilo.Core.Entities;

namespace Upkilo.Core.Interfaces;

public interface ITourService
{
    Task<UserTourProgress> GetProgressAsync(Guid userId, string tourKey);
    Task UpdateProgressAsync(Guid userId, string tourKey, int step, bool completed = false);
    Task ResetTourAsync(Guid userId, string tourKey);
    Task<IEnumerable<UserTourProgress>> GetAllToursProgressAsync(Guid userId);
}
