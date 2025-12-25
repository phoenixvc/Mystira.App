using Mystira.App.Application.Ports.Data;
using Mystira.Contracts.App.Responses.Badges;

namespace Mystira.App.Application.CQRS.Badges.Queries;

public sealed class GetProfileBadgeProgressQueryHandler : IQueryHandler<GetProfileBadgeProgressQuery, BadgeProgressResponse?>
{
    private readonly IBadgeRepository _badgeRepository;
    private readonly ICompassAxisRepository _axisRepository;
    private readonly IUserBadgeRepository _userBadgeRepository;
    private readonly IUserProfileRepository _profileRepository;

    public GetProfileBadgeProgressQueryHandler(
        IBadgeRepository badgeRepository,
        ICompassAxisRepository axisRepository,
        IUserBadgeRepository userBadgeRepository,
        IUserProfileRepository profileRepository)
    {
        _badgeRepository = badgeRepository;
        _axisRepository = axisRepository;
        _userBadgeRepository = userBadgeRepository;
        _profileRepository = profileRepository;
    }

    public async Task<BadgeProgressResponse?> Handle(GetProfileBadgeProgressQuery request, CancellationToken cancellationToken)
    {
        var profile = await _profileRepository.GetByIdAsync(request.ProfileId);
        if (profile == null) return null;

        var ageGroupId = profile.AgeGroup?.Value ?? "6-9";

        // Retrieve badges for the age group. Some Cosmos providers may attempt to use
        // ORDER BY in the query which requires a composite index. To avoid runtime
        // failures when composite indexes are not yet deployed, always perform
        // ordering in-memory here.
        var allBadges = await _badgeRepository.GetByAgeGroupAsync(ageGroupId);

        var badgesByAxis = allBadges
            .GroupBy(b => b.CompassAxisId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(b => b.TierOrder).ToList()
            );

        var userBadges = (await _userBadgeRepository.GetByUserProfileIdAsync(request.ProfileId)).ToList();

        var axes = await _axisRepository.GetAllAsync();
        var axisDictionary = axes.ToDictionary(a => a.Id, a => a);

        var response = new BadgeProgressResponse
        {
            AgeGroupId = ageGroupId,
            AxisProgresses = new List<AxisProgressResponse>()
        };

        foreach (var (axisId, badges) in badgesByAxis.OrderBy(x => x.Key))
        {
            var axis = axisDictionary.TryGetValue(axisId, out var a) ? a : null;
            var axisName = axis?.Name ?? axisId;

            // Derive current score for this axis from earned badges' values (max of TriggerValue/Threshold)
            // Match legacy data where Axis may store axis name instead of ID
            var axisUserBadges = userBadges
                .Where(ub => string.Equals(ub.Axis, axisId, StringComparison.OrdinalIgnoreCase)
                             || string.Equals(ub.Axis, axisName, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(ub => Math.Max(ub.TriggerValue, ub.Threshold))
                .ThenByDescending(ub => ub.EarnedAt)
                .ToList();

            var currentScore = axisUserBadges
                .Select(ub => Math.Max(ub.TriggerValue, ub.Threshold))
                .DefaultIfEmpty(0f)
                .Max();

            var axisTiers = new List<BadgeTierProgressResponse>();
            foreach (var badge in badges)
            {
                var matchedBadge = axisUserBadges
                    .FirstOrDefault(ub => Math.Max(ub.TriggerValue, ub.Threshold) >= badge.RequiredScore);
                var isEarned = matchedBadge != null;

                axisTiers.Add(new BadgeTierProgressResponse
                {
                    BadgeId = badge.Id,
                    Tier = badge.Tier,
                    TierOrder = badge.TierOrder,
                    Title = badge.Title,
                    Description = badge.Description,
                    RequiredScore = badge.RequiredScore,
                    ImageId = badge.ImageId,
                    IsEarned = isEarned,
                    EarnedAt = isEarned ? matchedBadge!.EarnedAt : null,
                    ProgressToThreshold = currentScore,
                    RemainingScore = Math.Max(0, badge.RequiredScore - currentScore)
                });
            }

            response.AxisProgresses.Add(new AxisProgressResponse
            {
                CompassAxisId = axisId,
                CompassAxisName = axisName,
                CurrentScore = (int)currentScore,
                Tiers = axisTiers
            });
        }

        return response;
    }
}
