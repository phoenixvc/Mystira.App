using Mystira.App.Application.CQRS.Auth.Responses;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.Auth.Commands;

/// <summary>
/// Command to verify a passwordless signin code and authenticate the user.
/// Returns account with JWT tokens upon successful verification.
/// </summary>
public record VerifyPasswordlessSigninCommand(
    string Email,
    string Code
) : ICommand<AuthResponse>;
