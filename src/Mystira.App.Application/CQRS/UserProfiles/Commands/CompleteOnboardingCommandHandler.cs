using Microsoft.Extensions.Logging;
using Mystira.App.Application.Interfaces;
using Mystira.App.Application.Ports.Data;

namespace Mystira.App.Application.CQRS.UserProfiles.Commands;

/// <summary>
/// Handler for CompleteOnboardingCommand
/// Marks the onboarding process as completed for a user profile
/// </summary>
public class CompleteOnboardingCommandHandler : ICommandHandler<CompleteOnboardingCommand, bool>
{
    private readonly IUserProfileRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CompleteOnboardingCommandHandler> _logger;

    public CompleteOnboardingCommandHandler(
        IUserProfileRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<CompleteOnboardingCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> Handle(
        CompleteOnboardingCommand command,
        CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByIdAsync(command.ProfileId);
        if (profile == null)
        {
            _logger.LogWarning("Profile not found: {ProfileId}", command.ProfileId);
            return false;
        }

        // Mark onboarding as complete
        profile.HasCompletedOnboarding = true;
        profile.UpdatedAt = DateTime.UtcNow;

        // Update in repository
        await _repository.UpdateAsync(profile);

        // Persist changes
        await _unitOfWork.CommitAsync(cancellationToken);

        _logger.LogInformation("Completed onboarding for profile {ProfileId}", command.ProfileId);

        return true;
    }
}
