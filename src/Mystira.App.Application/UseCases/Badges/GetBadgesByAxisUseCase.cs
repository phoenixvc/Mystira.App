using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.UseCases.Badges;

/// <summary>
/// Use case for retrieving badges for a user profile filtered by axis
/// </summary>
public class GetBadgesByAxisUseCase
{
    private readonly IUserBadgeRepository _repository;
    private readonly ILogger<GetBadgesByAxisUseCase> _logger;

    public GetBadgesByAxisUseCase(
        IUserBadgeRepository repository,
        ILogger<GetBadgesByAxisUseCase> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<List<UserBadge>> ExecuteAsync(string userProfileId, string axis)
    {
        if (string.IsNullOrWhiteSpace(userProfileId))
        {
            throw new ArgumentException("User profile ID cannot be null or empty", nameof(userProfileId));
        }

        if (string.IsNullOrWhiteSpace(axis))
        {
            throw new ArgumentException("Axis cannot be null or empty", nameof(axis));
        }

        var badges = await _repository.GetByUserProfileIdAndAxisAsync(userProfileId, axis);
        var badgeList = badges.ToList();

        _logger.LogInformation("Retrieved {Count} badges for user profile {UserProfileId} on axis {Axis}",
            badgeList.Count, userProfileId, axis);
        return badgeList;
    }
}

