using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.Domain.Models;

namespace Mystira.App.Application.CQRS.UserProfiles.Commands;

/// <summary>
/// Wolverine handler for UpdateUserProfileCommand.
/// Updates an existing user profile with new information.
/// </summary>
public static class UpdateUserProfileCommandHandler
{
    /// <summary>
    /// Handles the UpdateUserProfileCommand by updating a user profile.
    /// Wolverine injects dependencies as method parameters.
    /// </summary>
    public static async Task<UserProfile?> Handle(
        UpdateUserProfileCommand command,
        IUserProfileRepository repository,
        IUnitOfWork unitOfWork,
        ILogger logger,
        CancellationToken ct)
    {
        var profile = await repository.GetByIdAsync(command.ProfileId);
        if (profile == null)
        {
            logger.LogWarning("Profile not found: {ProfileId}", command.ProfileId);
            return null;
        }

        var request = command.Request;

        // Update profile fields
        if (request.PreferredFantasyThemes != null)
        {
            profile.PreferredFantasyThemes = request.PreferredFantasyThemes
                .Select(t => new FantasyTheme(t))
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(request.AgeGroup))
        {
            if (!AgeGroupConstants.AllAgeGroups.Contains(request.AgeGroup))
            {
                throw new ArgumentException($"Invalid age group: {request.AgeGroup}. Must be one of: {string.Join(", ", AgeGroupConstants.AllAgeGroups)}");
            }

            profile.AgeGroupName = request.AgeGroup;
        }

        if (request.DateOfBirth.HasValue)
        {
            profile.DateOfBirth = request.DateOfBirth;
            profile.UpdateAgeGroupFromBirthDate();
        }

        if (request.SelectedAvatarMediaId != null)
        {
            profile.SelectedAvatarMediaId = request.SelectedAvatarMediaId;
        }

        if (request.Pronouns != null)
        {
            profile.Pronouns = request.Pronouns;
        }

        if (request.Bio != null)
        {
            profile.Bio = request.Bio;
        }

        if (request.AccountId != null)
        {
            profile.AccountId = request.AccountId;
        }

        if (request.HasCompletedOnboarding.HasValue)
        {
            profile.HasCompletedOnboarding = request.HasCompletedOnboarding.Value;
        }

        if (request.IsGuest.HasValue)
        {
            profile.IsGuest = request.IsGuest.Value;
        }

        if (request.IsNpc.HasValue)
        {
            profile.IsNpc = request.IsNpc.Value;
        }

        profile.UpdatedAt = DateTime.UtcNow;

        // Update in repository
        await repository.UpdateAsync(profile);

        // Persist changes
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Updated user profile {ProfileId}", profile.Id);

        return profile;
    }
}
