using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Auth;
using Mystira.App.Application.Ports.Data;

namespace Mystira.App.Application.CQRS.Auth.Commands;

/// <summary>
/// Handler for refreshing JWT tokens.
/// Validates current access token, retrieves user account, and generates new tokens.
/// </summary>
public class RefreshTokenCommandHandler
    : ICommandHandler<RefreshTokenCommand, (bool Success, string Message, string? AccessToken, string? NewRefreshToken)>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IJwtService _jwtService;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    public RefreshTokenCommandHandler(
        IAccountRepository accountRepository,
        IJwtService jwtService,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _accountRepository = accountRepository;
        _jwtService = jwtService;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, string? AccessToken, string? NewRefreshToken)> Handle(
        RefreshTokenCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            // Extract user ID from the access token, allowing expired tokens
            // This is the refresh flow - the access token may be expired but we still
            // need to extract the user ID to issue new tokens
            var (isValid, userId) = _jwtService.ExtractUserIdIgnoringExpiry(command.Token);

            if (!isValid || string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Invalid access token during refresh attempt (signature/issuer/audience validation failed)");
                return (false, "Invalid access token", null, null);
            }

            // In a real implementation, you would validate the refresh token against stored data
            // For now, we'll just generate new tokens (this is a simplified approach)
            // In production, you should store refresh tokens in a database and validate them

            // Get user account info
            var account = await _accountRepository.GetByAuth0UserIdAsync(userId);
            if (account == null)
            {
                _logger.LogWarning("User not found during token refresh: {UserId}", userId);
                return (false, "User not found", null, null);
            }

            // Generate new tokens
            var newAccessToken = _jwtService.GenerateAccessToken(
                account.Auth0UserId,
                account.Email,
                account.DisplayName,
                account.Role);
            var newRefreshToken = _jwtService.GenerateRefreshToken();

            _logger.LogInformation("Token refreshed successfully for user: {UserId}", userId);

            return (true, "Token refreshed successfully", newAccessToken, newRefreshToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            return (false, "An error occurred while refreshing token", null, null);
        }
    }
}
