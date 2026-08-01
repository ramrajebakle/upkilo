using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

public class TourService : ITourService
{
    private readonly AppDbContext _context;

    public TourService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<UserTourProgress> GetProgressAsync(Guid userId, string tourKey)
    {
        var progress = await _context.Set<UserTourProgress>()
            .FirstOrDefaultAsync(p => p.UserId == userId && p.TourKey == tourKey);

        if (progress == null)
        {
            progress = new UserTourProgress
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TourKey = tourKey,
                CurrentStep = 0,
                IsCompleted = false
            };
            _context.Set<UserTourProgress>().Add(progress);
            await _context.SaveChangesAsync();
        }

        return progress;
    }

    public async Task UpdateProgressAsync(Guid userId, string tourKey, int step, bool completed = false)
    {
        var progress = await GetProgressAsync(userId, tourKey);

        progress.CurrentStep = step;
        progress.IsCompleted = completed;
        progress.LastInteractedAt = DateTime.UtcNow;

        if (completed && !progress.CompletedAt.HasValue)
        {
            progress.CompletedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }

    public async Task ResetTourAsync(Guid userId, string tourKey)
    {
        var progress = await _context.Set<UserTourProgress>()
            .FirstOrDefaultAsync(p => p.UserId == userId && p.TourKey == tourKey);

        if (progress != null)
        {
            progress.CurrentStep = 0;
            progress.IsCompleted = false;
            progress.CompletedAt = null;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<UserTourProgress>> GetAllToursProgressAsync(Guid userId)
    {
        return await _context.Set<UserTourProgress>()
            .Where(p => p.UserId == userId)
            .ToListAsync();
    }
}
