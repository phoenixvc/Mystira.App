using Mystira.App.Domain.Models;
using Mystira.Shared.Data.Repositories;

namespace Mystira.App.Application.Ports.Data;

public interface IAxisAchievementRepository : IRepository<AxisAchievement>
{
    Task<IEnumerable<AxisAchievement>> GetByAgeGroupAsync(string ageGroupId);
    Task<IEnumerable<AxisAchievement>> GetByCompassAxisAsync(string compassAxisId);
}
