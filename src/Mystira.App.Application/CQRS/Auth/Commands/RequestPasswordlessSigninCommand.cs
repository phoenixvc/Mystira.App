namespace Mystira.App.Application.CQRS.Auth.Commands;

/// <summary>
/// Command to request a passwordless signin code be sent to the user's email.
/// </summary>
public record RequestPasswordlessSigninCommand(
    string Email
) : ICommand<(bool Success, string Message, string? Code, string? ErrorDetails)>;
