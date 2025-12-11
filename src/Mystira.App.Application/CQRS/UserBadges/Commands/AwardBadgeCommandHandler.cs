using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.UserBadges.Commands;

public class AwardBadgeCommandHandler : ICommandHandler<AwardBadgeCommand, UserBadge>
{
    private readonly IUserBadgeRepository _repository;
    private readonly IBadgeRepository _badgeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AwardBadgeCommandHandler> _logger;

    public AwardBadgeCommandHandler(
        IUserBadgeRepository repository,
        IBadgeRepository badgeRepository,
        IUnitOfWork unitOfWork,
        ILogger<AwardBadgeCommandHandler> logger)
    {
        _repository = repository;
        _badgeRepository = badgeRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<UserBadge> Handle(AwardBadgeCommand command, CancellationToken cancellationToken)
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

        var badgeDefinition = await _badgeRepository.GetByIdAsync(request.BadgeConfigurationId);
        if (badgeDefinition == null)
        {
            throw new ArgumentException($"Badge not found: {request.BadgeConfigurationId}");
        }

        var badge = new UserBadge
        {
            Id = Guid.NewGuid().ToString(),
            UserProfileId = request.UserProfileId,
            BadgeConfigurationId = request.BadgeConfigurationId,
            BadgeId = badgeDefinition.Id,
            BadgeName = badgeDefinition.Title,
            BadgeMessage = badgeDefinition.Description,
            Axis = badgeDefinition.CompassAxisId,
            TriggerValue = request.TriggerValue,
            Threshold = badgeDefinition.RequiredScore,
            GameSessionId = request.GameSessionId,
            ScenarioId = request.ScenarioId,
            ImageId = badgeDefinition.ImageId,
            EarnedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(badge);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Awarded badge {BadgeId} to user profile {UserProfileId}",
            badge.Id, request.UserProfileId);

        return badge;
    }
}
