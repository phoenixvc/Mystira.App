using Mystira.App.Application.CQRS.Auth.Responses;

namespace Mystira.App.Application.CQRS.Auth.Commands;

/// <summary>
/// Command to refresh an expired JWT access token using a valid refresh token.
/// Returns new JWT tokens upon successful validation.
/// </summary>
public record RefreshTokenCommand(
    string Token,
    string RefreshToken
) : ICommand<AuthResponse>;
