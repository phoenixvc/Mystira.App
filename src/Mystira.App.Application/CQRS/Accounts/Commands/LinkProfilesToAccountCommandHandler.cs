using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;

namespace Mystira.App.Application.CQRS.Accounts.Commands;

/// <summary>
/// Handler for linking multiple user profiles to an account.
/// Updates both the account's profile list and each profile's account reference.
/// </summary>
public class LinkProfilesToAccountCommandHandler : ICommandHandler<LinkProfilesToAccountCommand, bool>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<LinkProfilesToAccountCommandHandler> _logger;

    public LinkProfilesToAccountCommandHandler(
        IAccountRepository accountRepository,
        IUserProfileRepository userProfileRepository,
        IUnitOfWork unitOfWork,
        ILogger<LinkProfilesToAccountCommandHandler> logger)
    {
        _accountRepository = accountRepository;
        _userProfileRepository = userProfileRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> Handle(LinkProfilesToAccountCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.AccountId))
        {
            _logger.LogWarning("Cannot link profiles: Account ID is null or empty");
            return false;
        }

        if (command.UserProfileIds == null || !command.UserProfileIds.Any())
        {
            _logger.LogWarning("Cannot link profiles: Profile IDs list is null or empty");
            return false;
        }

        var account = await _accountRepository.GetByIdAsync(command.AccountId);
        if (account == null)
        {
            _logger.LogWarning("Account not found: {AccountId}", command.AccountId);
            return false;
        }

        var linkedCount = 0;
        foreach (var profileId in command.UserProfileIds)
        {
            try
            {
                var profile = await _userProfileRepository.GetByIdAsync(profileId);
                if (profile == null)
                {
                    _logger.LogWarning("User profile not found: {ProfileId}", profileId);
                    continue;
                }

                // Check if profile is already linked
                if (profile.AccountId == command.AccountId)
                {
                    _logger.LogDebug("Profile {ProfileId} is already linked to account {AccountId}",
                        profileId, command.AccountId);
                    continue;
                }

                // Link profile to account
                profile.AccountId = command.AccountId;
                await _userProfileRepository.UpdateAsync(profile);

                // Add profile ID to account's profile list if not already present
                if (!account.UserProfileIds.Contains(profileId))
                {
                    account.UserProfileIds.Add(profileId);
                }

                linkedCount++;
                _logger.LogInformation("Linked profile {ProfileId} to account {AccountId}",
                    profileId, command.AccountId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error linking profile {ProfileId} to account {AccountId}",
                    profileId, command.AccountId);
            }
        }

        if (linkedCount > 0)
        {
            await _accountRepository.UpdateAsync(account);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully linked {LinkedCount} of {TotalCount} profiles to account {AccountId}",
                linkedCount, command.UserProfileIds.Count, command.AccountId);
        }

        return linkedCount > 0;
    }
}
