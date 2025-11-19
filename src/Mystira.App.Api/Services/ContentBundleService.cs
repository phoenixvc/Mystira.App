using Microsoft.EntityFrameworkCore;
using Mystira.App.Api.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Api.Services;

public class ContentBundleService : IContentBundleService
{
    private readonly MystiraAppDbContext _context;
    private readonly ILogger<ContentBundleService> _logger;

    public ContentBundleService(MystiraAppDbContext context, ILogger<ContentBundleService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<ContentBundle>> GetAllAsync()
    {
        return await _context.ContentBundles.AsNoTracking().ToListAsync();
    }

    public async Task<List<ContentBundle>> GetByAgeGroupAsync(string ageGroup)
    {
        return await _context.ContentBundles.AsNoTracking()
            .Where(b => b.AgeGroup == ageGroup)
            .ToListAsync();
    }
}
