using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.Contracts.App.Requests.Badges;
using Mystira.Domain.Models;

namespace Mystira.App.Application.UseCases.Badges;

public class AwardBadgeUseCase
{
    private readonly IUserBadgeRepository _badgeRepository;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IBadgeRepository _newBadgeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AwardBadgeUseCase> _logger;

    public AwardBadgeUseCase(
        IUserBadgeRepository badgeRepository,
        IUserProfileRepository userProfileRepository,
        IBadgeRepository newBadgeRepository,
        IUnitOfWork unitOfWork,
        ILogger<AwardBadgeUseCase> logger)
    {
        _badgeRepository = badgeRepository;
        _userProfileRepository = userProfileRepository;
        _newBadgeRepository = newBadgeRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<UserBadge> ExecuteAsync(AwardBadgeRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var existingBadge = await _badgeRepository.GetByUserProfileIdAndBadgeConfigIdAsync(
            request.UserProfileId, request.BadgeConfigurationId);

        if (existingBadge != null)
        {
            _logger.LogWarning("User {UserProfileId} already has badge {BadgeId}",
                request.UserProfileId, request.BadgeConfigurationId);
            return existingBadge;
        }

        var badge = await _newBadgeRepository.GetByIdAsync(request.BadgeConfigurationId);
        if (badge == null)
        {
            throw new ArgumentException($"Badge not found: {request.BadgeConfigurationId}", nameof(request));
        }

        var userProfile = await _userProfileRepository.GetByIdAsync(request.UserProfileId);
        if (userProfile == null)
        {
            throw new ArgumentException($"User profile not found: {request.UserProfileId}", nameof(request));
        }

        var newBadge = new UserBadge
        {
            Id = Guid.NewGuid().ToString(),
            UserProfileId = request.UserProfileId,
            BadgeConfigurationId = request.BadgeConfigurationId,
            BadgeId = badge.Id,
            BadgeName = badge.Title,
            BadgeMessage = badge.Description,
            Axis = badge.CompassAxisId,
            TriggerValue = request.TriggerValue,
            Threshold = badge.RequiredScore,
            GameSessionId = request.GameSessionId,
            ScenarioId = request.ScenarioId,
            ImageId = badge.ImageId,
            EarnedAt = DateTime.UtcNow
        };

        await _badgeRepository.AddAsync(newBadge);

        userProfile.AddEarnedBadge(newBadge);
        await _userProfileRepository.UpdateAsync(userProfile);

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Awarded badge {BadgeName} to user {UserProfileId}",
            badge.Title, request.UserProfileId);

        return newBadge;
    }
}
