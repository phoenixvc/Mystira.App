using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.Services;

/// <summary>
/// Awards badges based on axis score thresholds
/// </summary>
public class BadgeAwardingService : IBadgeAwardingService
{
    private readonly IBadgeRepository _badgeRepository;
    private readonly IUserBadgeRepository _userBadgeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<BadgeAwardingService> _logger;

    public BadgeAwardingService(
        IBadgeRepository badgeRepository,
        IUserBadgeRepository userBadgeRepository,
        IUnitOfWork unitOfWork,
        ILogger<BadgeAwardingService> logger)
    {
        _badgeRepository = badgeRepository;
        _userBadgeRepository = userBadgeRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<List<UserBadge>> AwardBadgesAsync(UserProfile profile, Dictionary<string, float> axisScores)
    {
        var newBadges = new List<UserBadge>();

        // Get the profile's age group
        var ageGroup = profile.AgeGroup;
        var ageGroupId = ageGroup?.Value ?? "6-9";

        // Get all badges for this age group
        var availableBadges = await _badgeRepository.GetByAgeGroupAsync(ageGroupId);

        // Get already earned badges for this profile
        var earnedBadges = await _userBadgeRepository.GetByUserProfileIdAsync(profile.Id);
        var earnedBadgeIds = new HashSet<string>(
            earnedBadges
                .Select(b => b.BadgeId)
                .Where(b => !string.IsNullOrEmpty(b))
                .Cast<string>()
                ?? new List<string>());

        // Build a normalized map of axis scores to be resilient to formatting differences
        // e.g., "Axis-Honesty" vs "honesty" vs "axis_honesty"
        static string NormalizeAxisKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return string.Empty;
            key = key.Trim();
            // Remove common prefixes
            if (key.StartsWith("axis-", StringComparison.OrdinalIgnoreCase)) key = key[5..];
            if (key.StartsWith("axis_", StringComparison.OrdinalIgnoreCase)) key = key[5..];
            // Remove separators
            key = key.Replace(" ", string.Empty)
                     .Replace("-", string.Empty)
                     .Replace("_", string.Empty);
            return key.ToLowerInvariant();
        }

        var normalizedAxisScores = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in axisScores)
        {
            var normalized = NormalizeAxisKey(kvp.Key);
            // Prefer exact axis key if there are collisions; otherwise store normalized
            if (!normalizedAxisScores.ContainsKey(normalized))
            {
                normalizedAxisScores[normalized] = kvp.Value;
            }
            else
            {
                // Sum if multiple variations collapse to the same normalized axis
                normalizedAxisScores[normalized] += kvp.Value;
            }
        }

        // Group badges by axis and tier, sorted by tier order
        var badgesByAxis = availableBadges
            .GroupBy(b => b.CompassAxisId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.OrderBy(b => b.TierOrder).ToList(), StringComparer.OrdinalIgnoreCase);

        // Evaluate each axis
        foreach (var (axis, badges) in badgesByAxis)
        {
            // Get the score for this axis (try direct key first, then normalized)
            var hasScore = axisScores.TryGetValue(axis, out var score);
            if (!hasScore)
            {
                var normalized = NormalizeAxisKey(axis);
                hasScore = normalizedAxisScores.TryGetValue(normalized, out score);
            }
            if (!hasScore)
            {
                continue;
            }

            // Find the highest tier the profile qualifies for (evaluate in tier order)
            foreach (var badge in badges)
            {
                // Skip if already earned
                if (earnedBadgeIds.Contains(badge.Id))
                {
                    continue;
                }

                // Check if score meets the threshold
                if (score >= badge.RequiredScore)
                {
                    var userBadge = new UserBadge
                    {
                        UserProfileId = profile.Id,
                        BadgeId = badge.Id,
                        BadgeConfigurationId = badge.Id, // For backward compatibility
                        BadgeName = badge.Title,
                        BadgeMessage = badge.Description,
                        Axis = axis,
                        TriggerValue = score,
                        Threshold = badge.RequiredScore,
                        EarnedAt = DateTime.UtcNow,
                        ImageId = badge.ImageId
                    };

                    await _userBadgeRepository.AddAsync(userBadge);
                    newBadges.Add(userBadge);
                    earnedBadgeIds.Add(badge.Id);

                    _logger.LogInformation(
                        "Awarded badge {BadgeId} ({BadgeTitle}) to profile {ProfileId} on axis {Axis}",
                        badge.Id, badge.Title, profile.Id, axis);
                }
                else
                {
                    // Since tiers are ordered, if we don't meet this threshold, we won't meet higher ones
                    break;
                }
            }
        }

        // Persist all new badges
        if (newBadges.Count > 0)
        {
            await _unitOfWork.SaveChangesAsync(CancellationToken.None);
            _logger.LogInformation(
                "Awarded {Count} badges to profile {ProfileId}",
                newBadges.Count, profile.Id);
        }

        return newBadges;
    }
}
