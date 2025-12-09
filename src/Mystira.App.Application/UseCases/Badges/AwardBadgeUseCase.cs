using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Contracts.Requests.Badges;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.UseCases.Badges;

/// <summary>
/// Use case for awarding a badge to a user profile
/// </summary>
public class AwardBadgeUseCase
{
    private readonly IUserBadgeRepository _badgeRepository;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IBadgeConfigurationRepository _badgeConfigRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AwardBadgeUseCase> _logger;

    public AwardBadgeUseCase(
        IUserBadgeRepository badgeRepository,
        IUserProfileRepository userProfileRepository,
        IBadgeConfigurationRepository badgeConfigRepository,
        IUnitOfWork unitOfWork,
        ILogger<AwardBadgeUseCase> logger)
    {
        _badgeRepository = badgeRepository;
        _userProfileRepository = userProfileRepository;
        _badgeConfigRepository = badgeConfigRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<UserBadge> ExecuteAsync(AwardBadgeRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        // Check if user already has this badge
        var existingBadge = await _badgeRepository.GetByUserProfileIdAndBadgeConfigIdAsync(
            request.UserProfileId, request.BadgeConfigurationId);

        if (existingBadge != null)
        {
            _logger.LogWarning("User {UserProfileId} already has badge {BadgeId}",
                request.UserProfileId, request.BadgeConfigurationId);
            return existingBadge;
        }

        // Get badge configuration
        var badgeConfig = await _badgeConfigRepository.GetByIdAsync(request.BadgeConfigurationId);
        if (badgeConfig == null)
        {
            throw new ArgumentException($"Badge configuration not found: {request.BadgeConfigurationId}", nameof(request));
        }

        // Verify user profile exists
        var userProfile = await _userProfileRepository.GetByIdAsync(request.UserProfileId);
        if (userProfile == null)
        {
            throw new ArgumentException($"User profile not found: {request.UserProfileId}", nameof(request));
        }

        // Create new badge
        var newBadge = new UserBadge
        {
            Id = Guid.NewGuid().ToString(),
            UserProfileId = request.UserProfileId,
            BadgeConfigurationId = request.BadgeConfigurationId,
            BadgeName = badgeConfig.Name,
            BadgeMessage = badgeConfig.Message,
            Axis = badgeConfig.Axis,
            TriggerValue = request.TriggerValue,
            Threshold = badgeConfig.Threshold,
            GameSessionId = request.GameSessionId,
            ScenarioId = request.ScenarioId,
            ImageId = badgeConfig.ImageId,
            EarnedAt = DateTime.UtcNow
        };

        await _badgeRepository.AddAsync(newBadge);

        // Also add to user profile's earned badges list
        userProfile.AddEarnedBadge(newBadge);
        await _userProfileRepository.UpdateAsync(userProfile);

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Awarded badge {BadgeName} to user {UserProfileId}",
            badgeConfig.Name, request.UserProfileId);

        return newBadge;
    }
}

