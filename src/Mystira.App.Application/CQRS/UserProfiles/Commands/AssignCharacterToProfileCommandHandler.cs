using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;

namespace Mystira.App.Application.CQRS.UserProfiles.Commands;

/// <summary>
/// Wolverine handler for assigning a character to a user profile.
/// Validates that both the profile and character exist before assignment.
/// </summary>
public static class AssignCharacterToProfileCommandHandler
{
    /// <summary>
    /// Handles the AssignCharacterToProfileCommand by assigning a character to a profile.
    /// Wolverine injects dependencies as method parameters.
    /// </summary>
    public static async Task<bool> Handle(
        AssignCharacterToProfileCommand command,
        IUserProfileRepository profileRepository,
        ICharacterMapRepository characterRepository,
        IUnitOfWork unitOfWork,
        ILogger logger,
        CancellationToken ct)
    {
        // Validate profile exists
        var profile = await profileRepository.GetByIdAsync(command.ProfileId);
        if (profile == null)
        {
            logger.LogWarning("Cannot assign character: Profile not found {ProfileId}", command.ProfileId);
            return false;
        }

        // Validate character exists
        var character = await characterRepository.GetByIdAsync(command.CharacterId);
        if (character == null)
        {
            logger.LogWarning("Cannot assign character: Character not found {CharacterId}", command.CharacterId);
            return false;
        }

        // Update profile with character assignment
        profile.IsNpc = command.IsNpc;
        profile.UpdatedAt = DateTime.UtcNow;

        await profileRepository.UpdateAsync(profile);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Assigned character {CharacterId} to profile {ProfileId} (NPC: {IsNpc})",
            command.CharacterId, command.ProfileId, command.IsNpc);

        return true;
    }
}
