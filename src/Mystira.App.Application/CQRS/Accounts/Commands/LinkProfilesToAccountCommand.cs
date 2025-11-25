using Mystira.App.Application.Interfaces;

namespace Mystira.App.Application.CQRS.Accounts.Commands;

/// <summary>
/// Command to link multiple user profiles to an account.
/// </summary>
public record LinkProfilesToAccountCommand(
    string AccountId,
    List<string> UserProfileIds
) : ICommand<bool>;
