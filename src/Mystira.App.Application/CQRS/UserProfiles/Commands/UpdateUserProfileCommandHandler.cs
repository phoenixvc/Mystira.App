using Microsoft.Extensions.Logging;
using Mystira.App.Application.Interfaces;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.UserProfiles.Commands;

/// <summary>
/// Handler for UpdateUserProfileCommand
/// Updates an existing user profile with new information
/// </summary>
public class UpdateUserProfileCommandHandler : ICommandHandler<UpdateUserProfileCommand, UserProfile?>
{
    private readonly IUserProfileRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateUserProfileCommandHandler> _logger;

    public UpdateUserProfileCommandHandler(
        IUserProfileRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateUserProfileCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<UserProfile?> Handle(
        UpdateUserProfileCommand command,
        CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByIdAsync(command.ProfileId);
        if (profile == null)
        {
            _logger.LogWarning("Profile not found: {ProfileId}", command.ProfileId);
            return null;
        }

        var request = command.Request;

        // Update profile fields
        if (request.DateOfBirth.HasValue)
        {
            profile.DateOfBirth = request.DateOfBirth;
            profile.UpdateAgeGroupFromBirthDate();
        }

        if (request.PreferredFantasyThemes != null)
        {
            profile.PreferredFantasyThemes = request.PreferredFantasyThemes
                .Select(t => new FantasyTheme(t))
                .ToList();
        }

        if (request.SelectedAvatarMediaId != null)
            profile.SelectedAvatarMediaId = request.SelectedAvatarMediaId;

        if (request.Pronouns != null)
            profile.Pronouns = request.Pronouns;

        if (request.Bio != null)
            profile.Bio = request.Bio;

        if (request.AccountId != null)
            profile.AccountId = request.AccountId;

        if (request.HasCompletedOnboarding.HasValue)
            profile.HasCompletedOnboarding = request.HasCompletedOnboarding.Value;

        if (request.IsGuest.HasValue)
            profile.IsGuest = request.IsGuest.Value;

        if (request.IsNpc.HasValue)
            profile.IsNpc = request.IsNpc.Value;

        profile.UpdatedAt = DateTime.UtcNow;

        // Update in repository
        await _repository.UpdateAsync(profile);

        // Persist changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated user profile {ProfileId}", profile.Id);

        return profile;
    }
}
