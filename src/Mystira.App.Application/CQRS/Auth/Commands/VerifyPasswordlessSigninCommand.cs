using Mystira.App.Application.Interfaces;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.Auth.Commands;

/// <summary>
/// Command to verify a passwordless signin code and authenticate the user.
/// Returns account with JWT tokens upon successful verification.
/// </summary>
public record VerifyPasswordlessSigninCommand(
    string Email,
    string Code
) : ICommand<(bool Success, string Message, Account? Account, string? AccessToken, string? RefreshToken, string? ErrorDetails)>;
