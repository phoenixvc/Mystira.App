using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.UserBadges.Commands;

public class AwardBadgeCommandHandler : ICommandHandler<AwardBadgeCommand, UserBadge>
{
    private readonly IUserBadgeRepository _repository;
    private readonly IBadgeConfigurationRepository _badgeConfigRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AwardBadgeCommandHandler> _logger;

    public AwardBadgeCommandHandler(
        IUserBadgeRepository repository,
        IBadgeConfigurationRepository badgeConfigRepository,
        IUnitOfWork unitOfWork,
        ILogger<AwardBadgeCommandHandler> logger)
    {
        _repository = repository;
        _badgeConfigRepository = badgeConfigRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<UserBadge> Handle(AwardBadgeCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;

        if (string.IsNullOrEmpty(request.UserProfileId))
            throw new ArgumentException("UserProfileId is required");
        if (string.IsNullOrEmpty(request.BadgeConfigurationId))
            throw new ArgumentException("BadgeConfigurationId is required");

        // Get badge configuration to populate badge details
        var badgeConfig = await _badgeConfigRepository.GetByIdAsync(request.BadgeConfigurationId);
        if (badgeConfig == null)
            throw new ArgumentException($"Badge configuration not found: {request.BadgeConfigurationId}");

        var badge = new UserBadge
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

        await _repository.AddAsync(badge);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Awarded badge {BadgeId} to user profile {UserProfileId}",
            badge.Id, request.UserProfileId);

        return badge;
    }
}
