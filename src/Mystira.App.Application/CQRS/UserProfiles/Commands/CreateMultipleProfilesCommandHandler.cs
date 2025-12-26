using Microsoft.Extensions.Logging;
using Mystira.App.Domain.Models;
using Wolverine;

namespace Mystira.App.Application.CQRS.UserProfiles.Commands;

/// <summary>
/// Wolverine handler for creating multiple user profiles in a batch operation.
/// Delegates to CreateUserProfileCommand for each individual profile.
/// Used during onboarding when creating profiles for family members.
/// </summary>
public static class CreateMultipleProfilesCommandHandler
{
    /// <summary>
    /// Handles the CreateMultipleProfilesCommand by creating multiple profiles in batch.
    /// Wolverine injects dependencies as method parameters.
    /// </summary>
    public static async Task<List<UserProfile>> Handle(
        CreateMultipleProfilesCommand command,
        IMessageBus messageBus,
        ILogger logger,
        CancellationToken ct)
    {
        var createdProfiles = new List<UserProfile>();

        foreach (var profileRequest in command.Request.Profiles)
        {
            try
            {
                var createCommand = new CreateUserProfileCommand(profileRequest);
                var profile = await messageBus.InvokeAsync<UserProfile>(createCommand, ct);
                createdProfiles.Add(profile);

                logger.LogInformation("Created profile {ProfileId} with name {Name} in batch",
                    profile.Id, profile.Name);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to create profile {Name} in batch", profileRequest.Name);
                // Continue with other profiles - partial success is acceptable
            }
        }

        logger.LogInformation("Created {Count} of {Total} profiles in batch",
            createdProfiles.Count, command.Request.Profiles.Count);

        return createdProfiles;
    }
}
