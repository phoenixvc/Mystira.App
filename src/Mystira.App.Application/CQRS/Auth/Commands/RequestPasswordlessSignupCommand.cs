using Mystira.App.Application.CQRS.Auth.Responses;

namespace Mystira.App.Application.CQRS.Auth.Commands;

/// <summary>
/// Command to request a passwordless signup code be sent to the user's email.
/// </summary>
public record RequestPasswordlessSignupCommand(
    string Email,
    string DisplayName
) : ICommand<AuthResponse>;
