using Microsoft.EntityFrameworkCore;
using Mystira.App.Admin.Api.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Admin.Api.Services;

public class ContentBundleAdminService : IContentBundleAdminService
{
    private readonly MystiraAppDbContext _context;
    private readonly ILogger<ContentBundleAdminService> _logger;

    public ContentBundleAdminService(MystiraAppDbContext context, ILogger<ContentBundleAdminService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<ContentBundle>> GetAllAsync()
    {
        return await _context.ContentBundles.AsNoTracking().ToListAsync();
    }

    public async Task<ContentBundle?> GetByIdAsync(string id)
    {
        return await _context.ContentBundles.FindAsync(id);
    }

    public async Task<ContentBundle> CreateAsync(ContentBundle bundle)
    {
        if (string.IsNullOrWhiteSpace(bundle.Id))
        {
            bundle.Id = Guid.NewGuid().ToString("N");
        }
        _context.ContentBundles.Add(bundle);
        await _context.SaveChangesAsync();
        return bundle;
    }

    public async Task<ContentBundle?> UpdateAsync(string id, ContentBundle bundle)
    {
        var existing = await _context.ContentBundles.FindAsync(id);
        if (existing == null)
        {
            return null;
        }

        existing.Title = bundle.Title;
        existing.Description = bundle.Description;
        existing.ImageId = bundle.ImageId;
        existing.ScenarioIds = bundle.ScenarioIds?.ToList() ?? new List<string>();
        existing.Prices = bundle.Prices?.Select(p => new BundlePrice { Value = p.Value, Currency = p.Currency }).ToList() ?? new List<BundlePrice>();
        existing.IsFree = bundle.IsFree;
        existing.AgeGroup = bundle.AgeGroup;

        await _context.SaveChangesAsync();
        return existing;
    }
}
