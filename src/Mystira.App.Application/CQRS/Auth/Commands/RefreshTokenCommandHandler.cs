using Microsoft.Extensions.Logging;
using Mystira.App.Application.CQRS.Auth.Responses;
using Mystira.App.Application.Ports.Auth;
using Mystira.App.Application.Ports.Data;

namespace Mystira.App.Application.CQRS.Auth.Commands;

/// <summary>
/// Handler for refreshing JWT tokens.
/// Validates current access token, retrieves user account, and generates new tokens.
/// </summary>
public class RefreshTokenCommandHandler
    : ICommandHandler<RefreshTokenCommand, AuthResponse>
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

    public async Task<AuthResponse> Handle(
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
                return new AuthResponse(false, "Invalid access token");
            }

            // In a real implementation, you would validate the refresh token against stored data
            // For now, we'll just generate new tokens (this is a simplified approach)
            // In production, you should store refresh tokens in a database and validate them

            // Get user account info
            var account = await _accountRepository.GetByIdAsync(userId);
            if (account == null)
            {
                _logger.LogWarning("User not found during token refresh: {UserId}", userId);
                return new AuthResponse(false, "User not found");
            }

            // Generate new tokens
            var newAccessToken = _jwtService.GenerateAccessToken(
                account.Id,
                account.Email,
                account.DisplayName,
                account.Role);
            var newRefreshToken = _jwtService.GenerateRefreshToken();

            _logger.LogInformation("Token refreshed successfully for user: {UserId}", userId);

            return new AuthResponse(true, "Token refreshed successfully", null, null, null, newAccessToken, newRefreshToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            return new AuthResponse(false, "An error occurred while refreshing token");
        }
    }
}
