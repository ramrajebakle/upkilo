using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

public class LandingPageService : ILandingPageService
{
    private readonly AppDbContext _context;

    public LandingPageService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<LandingPage> CreatePageAsync(Guid tenantId, string title, string slug, string htmlContent, Guid? campaignId)
    {
        var page = new LandingPage
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Title = title,
            Slug = slug,
            HtmlContent = htmlContent,
            CampaignId = campaignId,
            IsPublished = false
        };
        _context.LandingPages.Add(page);
        await _context.SaveChangesAsync();
        return page;
    }

    public async Task<LandingPage?> GetPageBySlugAsync(string slug)
    {
        return await _context.LandingPages.FirstOrDefaultAsync(p => p.Slug == slug && p.IsPublished);
    }

    public async Task<IEnumerable<LandingPage>> GetPagesAsync(Guid tenantId)
    {
        return await _context.LandingPages.Where(p => p.TenantId == tenantId).ToListAsync();
    }

    public async Task<bool> PublishPageAsync(Guid tenantId, Guid pageId)
    {
        var page = await _context.LandingPages.FirstOrDefaultAsync(p => p.Id == pageId && p.TenantId == tenantId);
        if (page == null) return false;
        page.IsPublished = true;
        page.PublishedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RecordViewAsync(Guid pageId)
    {
        var page = await _context.LandingPages.FindAsync(pageId);
        if (page == null) return false;
        page.Views++;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RecordConversionAsync(Guid pageId)
    {
        var page = await _context.LandingPages.FindAsync(pageId);
        if (page == null) return false;
        page.Conversions++;
        await _context.SaveChangesAsync();
        return true;
    }
}
