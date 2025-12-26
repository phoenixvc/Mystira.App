using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;

namespace Mystira.App.Application.CQRS.Accounts.Commands;

/// <summary>
/// Wolverine handler for LinkProfilesToAccountCommand.
/// Links multiple user profiles to an account.
/// Updates both the account's profile list and each profile's account reference.
/// Uses static method convention for cleaner, more testable code.
/// </summary>
public static class LinkProfilesToAccountCommandHandler
{
    /// <summary>
    /// Handles the LinkProfilesToAccountCommand by linking user profiles to an account.
    /// Wolverine injects dependencies as method parameters.
    /// </summary>
    public static async Task<bool> Handle(
        LinkProfilesToAccountCommand command,
        IAccountRepository accountRepository,
        IUserProfileRepository userProfileRepository,
        IUnitOfWork unitOfWork,
        ILogger logger,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.AccountId))
        {
            logger.LogWarning("Cannot link profiles: Account ID is null or empty");
            return false;
        }

        if (command.UserProfileIds == null || !command.UserProfileIds.Any())
        {
            logger.LogWarning("Cannot link profiles: Profile IDs list is null or empty");
            return false;
        }

        var account = await accountRepository.GetByIdAsync(command.AccountId);
        if (account == null)
        {
            logger.LogWarning("Account not found: {AccountId}", command.AccountId);
            return false;
        }

        var linkedCount = 0;
        foreach (var profileId in command.UserProfileIds)
        {
            try
            {
                var profile = await userProfileRepository.GetByIdAsync(profileId);
                if (profile == null)
                {
                    logger.LogWarning("User profile not found: {ProfileId}", profileId);
                    continue;
                }

                // Check if profile is already linked
                if (profile.AccountId == command.AccountId)
                {
                    logger.LogDebug("Profile {ProfileId} is already linked to account {AccountId}",
                        profileId, command.AccountId);
                    continue;
                }

                // Link profile to account
                profile.AccountId = command.AccountId;
                await userProfileRepository.UpdateAsync(profile);

                // Add profile ID to account's profile list if not already present
                if (!account.UserProfileIds.Contains(profileId))
                {
                    account.UserProfileIds.Add(profileId);
                }

                linkedCount++;
                logger.LogInformation("Linked profile {ProfileId} to account {AccountId}",
                    profileId, command.AccountId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error linking profile {ProfileId} to account {AccountId}",
                    profileId, command.AccountId);
            }
        }

        if (linkedCount > 0)
        {
            await accountRepository.UpdateAsync(account);
            await unitOfWork.SaveChangesAsync(ct);

            logger.LogInformation("Successfully linked {LinkedCount} of {TotalCount} profiles to account {AccountId}",
                linkedCount, command.UserProfileIds.Count, command.AccountId);
        }

        return linkedCount > 0;
    }
}
