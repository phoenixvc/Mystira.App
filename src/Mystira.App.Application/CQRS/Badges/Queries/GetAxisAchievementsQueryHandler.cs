using Mystira.App.Application.Ports.Data;
using Mystira.App.Contracts.Responses.Badges;

namespace Mystira.App.Application.CQRS.Badges.Queries;

public sealed class GetAxisAchievementsQueryHandler : IQueryHandler<GetAxisAchievementsQuery, List<AxisAchievementResponse>>
{
    private readonly IAxisAchievementRepository _axisAchievementRepository;
    private readonly ICompassAxisRepository _axisRepository;

    public GetAxisAchievementsQueryHandler(
        IAxisAchievementRepository axisAchievementRepository,
        ICompassAxisRepository axisRepository)
    {
        _axisAchievementRepository = axisAchievementRepository;
        _axisRepository = axisRepository;
    }

    public async Task<List<AxisAchievementResponse>> Handle(GetAxisAchievementsQuery request, CancellationToken cancellationToken)
    {
        var achievements = await _axisAchievementRepository.GetByAgeGroupAsync(request.AgeGroupId);
        var axes = await _axisRepository.GetAllAsync();

        var axisLookup = axes
            .SelectMany(a => new[] { (Key: a.Id, Value: a), (Key: a.Name, Value: a) })
            .Where(x => !string.IsNullOrWhiteSpace(x.Key))
            .ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);

        return achievements
            .OrderBy(a => a.CompassAxisId)
            .ThenBy(a => a.AxesDirection)
            .Select(a =>
            {
                axisLookup.TryGetValue(a.CompassAxisId, out var axis);
                var axisName = axis != null && !string.IsNullOrWhiteSpace(axis.Name)
                    ? axis.Name
                    : (axis?.Id ?? a.CompassAxisId);

                return new AxisAchievementResponse
                {
                    Id = a.Id,
                    AgeGroupId = a.AgeGroupId,
                    CompassAxisId = a.CompassAxisId,
                    CompassAxisName = axisName,
                    AxesDirection = a.AxesDirection,
                    Description = a.Description
                };
            })
            .ToList();
    }
}
