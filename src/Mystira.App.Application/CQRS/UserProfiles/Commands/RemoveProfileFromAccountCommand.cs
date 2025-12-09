namespace Mystira.App.Application.CQRS.UserProfiles.Commands;

/// <summary>
/// Command to remove a user profile from its associated account.
/// Clears the profile's AccountId and removes the profile ID from the account's profile list.
/// </summary>
public record RemoveProfileFromAccountCommand(string ProfileId) : ICommand<bool>;
