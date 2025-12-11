using Mystira.App.Domain.Models;

namespace Mystira.App.Application.Ports.Data;

public interface IAxisAchievementRepository : IRepository<AxisAchievement>
{
    Task<IEnumerable<AxisAchievement>> GetByAgeGroupAsync(string ageGroupId);
    Task<IEnumerable<AxisAchievement>> GetByCompassAxisAsync(string compassAxisId);
}
