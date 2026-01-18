using Ardalis.Specification;
using Mystira.App.Domain.Models;
using Mystira.Shared.Data.Repositories;

namespace Mystira.App.Application.Ports.Data;

public interface IAxisAchievementRepository : IRepository<AxisAchievement, string>, IRepositoryBase<AxisAchievement>
{
    Task<IEnumerable<AxisAchievement>> GetByAgeGroupAsync(string ageGroupId);
    Task<IEnumerable<AxisAchievement>> GetByCompassAxisAsync(string compassAxisId);
}
