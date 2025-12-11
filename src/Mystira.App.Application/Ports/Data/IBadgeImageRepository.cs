using Mystira.App.Domain.Models;

namespace Mystira.App.Application.Ports.Data;

public interface IBadgeImageRepository : IRepository<BadgeImage>
{
    Task<BadgeImage?> GetByImageIdAsync(string imageId);
}
