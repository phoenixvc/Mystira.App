using Microsoft.Extensions.Logging;
using Mystira.App.Application.Interfaces;
using Mystira.App.Application.Ports.Data;

namespace Mystira.App.Application.CQRS.UserProfiles.Commands;

/// <summary>
/// Handler for removing a user profile from its associated account.
/// Coordinates updates to both the profile (clears AccountId) and account (removes profile from list).
/// </summary>
public class RemoveProfileFromAccountCommandHandler : ICommandHandler<RemoveProfileFromAccountCommand, bool>
{
    private readonly IUserProfileRepository _profileRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RemoveProfileFromAccountCommandHandler> _logger;

    public RemoveProfileFromAccountCommandHandler(
        IUserProfileRepository profileRepository,
        IAccountRepository accountRepository,
        IUnitOfWork unitOfWork,
        ILogger<RemoveProfileFromAccountCommandHandler> logger)
    {
        _profileRepository = profileRepository;
        _accountRepository = accountRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> Handle(
        RemoveProfileFromAccountCommand command,
        CancellationToken cancellationToken)
    {
        // Get the profile
        var profile = await _profileRepository.GetByIdAsync(command.ProfileId);
        if (profile == null)
        {
            _logger.LogWarning("Cannot remove profile from account: Profile not found {ProfileId}",
                command.ProfileId);
            return false;
        }

        // Check if profile is linked to an account
        if (string.IsNullOrEmpty(profile.AccountId))
        {
            _logger.LogInformation("Profile {ProfileId} is not linked to any account", command.ProfileId);
            return true; // Already unlinked, consider this success
        }

        var accountId = profile.AccountId;

        // Get the account
        var account = await _accountRepository.GetByIdAsync(accountId);
        if (account != null)
        {
            // Remove profile ID from account's profile list
            if (account.UserProfileIds.Contains(command.ProfileId))
            {
                account.UserProfileIds.Remove(command.ProfileId);
                await _accountRepository.UpdateAsync(account);

                _logger.LogInformation("Removed profile {ProfileId} from account {AccountId}",
                    command.ProfileId, accountId);
            }
        }
        else
        {
            _logger.LogWarning("Account {AccountId} not found, but profile still linked to it", accountId);
        }

        // Clear profile's account ID
        profile.AccountId = null;
        profile.UpdatedAt = DateTime.UtcNow;
        await _profileRepository.UpdateAsync(profile);

        // Save all changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully removed profile {ProfileId} from account {AccountId}",
            command.ProfileId, accountId);

        return true;
    }
}
