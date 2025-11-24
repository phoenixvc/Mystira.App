using Microsoft.Extensions.Logging;
using Mystira.App.Application.Interfaces;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.UserBadges.Commands;

public class AwardBadgeCommandHandler : ICommandHandler<AwardBadgeCommand, UserBadge>
{
    private readonly IUserBadgeRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AwardBadgeCommandHandler> _logger;

    public AwardBadgeCommandHandler(
        IUserBadgeRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<AwardBadgeCommandHandler> logger)
    {
        _repository = repository;
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

        var badge = new UserBadge
        {
            Id = Guid.NewGuid().ToString(),
            UserProfileId = request.UserProfileId,
            BadgeConfigurationId = request.BadgeConfigurationId,
            Axis = request.Axis,
            EarnedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(badge);
        await _unitOfWork.CommitAsync(cancellationToken);

        _logger.LogInformation("Awarded badge {BadgeId} to user profile {UserProfileId}",
            badge.Id, request.UserProfileId);

        return badge;
    }
}
