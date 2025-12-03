namespace Mystira.App.Application.CQRS.UserProfiles.Commands;

/// <summary>
/// Command to assign a character to a user profile.
/// Updates the profile's IsNpc flag based on the assignment type.
/// </summary>
public record AssignCharacterToProfileCommand(
    string ProfileId,
    string CharacterId,
    bool IsNpc = false
) : ICommand<bool>;
