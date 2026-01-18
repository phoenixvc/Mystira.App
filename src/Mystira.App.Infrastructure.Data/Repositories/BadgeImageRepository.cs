using Microsoft.EntityFrameworkCore;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Infrastructure.Data.Repositories;

public class BadgeImageRepository : Repository<BadgeImage>, IBadgeImageRepository
{
    public BadgeImageRepository(MystiraAppDbContext context) : base(context)
    {
    }

    public async Task<BadgeImage?> GetByImageIdAsync(string imageId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(x => x.ImageId == imageId);
    }
}
