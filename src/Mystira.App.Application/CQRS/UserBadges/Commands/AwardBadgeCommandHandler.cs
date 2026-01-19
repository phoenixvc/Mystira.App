using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.UserBadges.Commands;

/// <summary>
/// Wolverine handler for AwardBadgeCommand.
/// Awards a badge to a user profile - this is a write operation that modifies state.
/// </summary>
public static class AwardBadgeCommandHandler
{
    /// <summary>
    /// Handles the AwardBadgeCommand by creating a user badge in the repository.
    /// Wolverine injects dependencies as method parameters.
    /// </summary>
    public static async Task<UserBadge> Handle(
        AwardBadgeCommand command,
        IUserBadgeRepository repository,
        IBadgeConfigurationRepository badgeConfigRepository,
        IUnitOfWork unitOfWork,
        ILogger logger,
        CancellationToken ct)
    {
        var request = command.Request;

        if (string.IsNullOrEmpty(request.UserProfileId))
        {
            throw new ArgumentException("UserProfileId is required");
        }

        if (string.IsNullOrEmpty(request.BadgeConfigurationId))
        {
            throw new ArgumentException("BadgeConfigurationId is required");
        }

        var badgeConfig = await badgeConfigRepository.GetByIdAsync(request.BadgeConfigurationId);
        if (badgeConfig == null)
        {
            throw new ArgumentException($"Badge not found: {request.BadgeConfigurationId}");
        }

        var badge = new UserBadge
        {
            Id = Guid.NewGuid().ToString(),
            UserProfileId = request.UserProfileId,
            BadgeConfigurationId = request.BadgeConfigurationId,
            BadgeId = badgeConfig.Id,
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

        await repository.AddAsync(badge);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Awarded badge {BadgeId} to user profile {UserProfileId}",
            badge.Id, request.UserProfileId);

        return badge;
    }
}
