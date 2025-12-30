using Mystira.Domain.Models;

namespace Mystira.App.Application.Ports.Data;

public interface IBadgeRepository : IRepository<Badge>
{
    Task<IEnumerable<Badge>> GetByAgeGroupAsync(string ageGroupId);
    Task<IEnumerable<Badge>> GetByCompassAxisAsync(string compassAxisId);
    Task<Badge?> GetByAgeGroupAxisAndTierAsync(string ageGroupId, string compassAxisId, int tierOrder);
}
