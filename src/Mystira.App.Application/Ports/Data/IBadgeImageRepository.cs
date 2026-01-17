using Mystira.App.Domain.Models;
using Mystira.Shared.Data.Repositories;

namespace Mystira.App.Application.Ports.Data;

public interface IBadgeImageRepository : IRepository<BadgeImage>
{
    Task<BadgeImage?> GetByImageIdAsync(string imageId);
}
