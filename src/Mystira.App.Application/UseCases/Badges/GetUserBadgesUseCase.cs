using Microsoft.Extensions.Logging;
using Mystira.App.Domain.Models;
using Mystira.App.Infrastructure.Data.Repositories;

namespace Mystira.App.Application.UseCases.Badges;

/// <summary>
/// Use case for retrieving badges for a user profile
/// </summary>
public class GetUserBadgesUseCase
{
    private readonly IUserBadgeRepository _repository;
    private readonly ILogger<GetUserBadgesUseCase> _logger;

    public GetUserBadgesUseCase(
        IUserBadgeRepository repository,
        ILogger<GetUserBadgesUseCase> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<List<UserBadge>> ExecuteAsync(string userProfileId)
    {
        if (string.IsNullOrWhiteSpace(userProfileId))
        {
            throw new ArgumentException("User profile ID cannot be null or empty", nameof(userProfileId));
        }

        var badges = await _repository.GetByUserProfileIdAsync(userProfileId);
        var badgeList = badges.ToList();

        _logger.LogInformation("Retrieved {Count} badges for user profile {UserProfileId}", badgeList.Count, userProfileId);
        return badgeList;
    }
}

