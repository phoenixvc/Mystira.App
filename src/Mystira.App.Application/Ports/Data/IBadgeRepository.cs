using Ardalis.Specification;
using Mystira.App.Domain.Models;
using Mystira.Shared.Data.Repositories;

namespace Mystira.App.Application.Ports.Data;

public interface IBadgeRepository : IRepository<Badge, string>, IRepositoryBase<Badge>
{
    Task<IEnumerable<Badge>> GetByAgeGroupAsync(string ageGroupId);
    Task<IEnumerable<Badge>> GetByCompassAxisAsync(string compassAxisId);
    Task<Badge?> GetByAgeGroupAxisAndTierAsync(string ageGroupId, string compassAxisId, int tierOrder);
}
