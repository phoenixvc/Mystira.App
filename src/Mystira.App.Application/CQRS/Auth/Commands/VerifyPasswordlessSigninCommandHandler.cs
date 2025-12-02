using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Auth;
using Mystira.App.Application.Interfaces;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.Auth.Commands;

/// <summary>
/// Handler for verifying passwordless signin code and authenticating user.
/// Validates code with rate limiting, updates last login, and generates JWT tokens.
/// </summary>
public class VerifyPasswordlessSigninCommandHandler
    : ICommandHandler<VerifyPasswordlessSigninCommand, (bool Success, string Message, Account? Account, string? AccessToken, string? RefreshToken, string? ErrorDetails)>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IPendingSignupRepository _pendingSignupRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtService _jwtService;
    private readonly ILogger<VerifyPasswordlessSigninCommandHandler> _logger;
    private const int MaxFailedAttempts = 5;

    public VerifyPasswordlessSigninCommandHandler(
        IAccountRepository accountRepository,
        IPendingSignupRepository pendingSignupRepository,
        IUnitOfWork unitOfWork,
        IJwtService jwtService,
        ILogger<VerifyPasswordlessSigninCommandHandler> logger)
    {
        _accountRepository = accountRepository;
        _pendingSignupRepository = pendingSignupRepository;
        _unitOfWork = unitOfWork;
        _jwtService = jwtService;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, Account? Account, string? AccessToken, string? RefreshToken, string? ErrorDetails)> Handle(
        VerifyPasswordlessSigninCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var email = command.Email.ToLowerInvariant().Trim();
            var code = command.Code.Trim();

            // Try to find pending signin with matching email and code
            var pendingSignin = await _pendingSignupRepository.GetByEmailAndCodeAsync(email, code);
            if (pendingSignin == null || !pendingSignin.IsSignin)
            {
                // Check if there's a pending signin for this email (code might be wrong)
                var pendingByEmail = await _pendingSignupRepository.GetActiveByEmailAsync(email);
                if (pendingByEmail != null && pendingByEmail.IsSignin && !pendingByEmail.IsUsed)
                {
                    if (pendingByEmail.FailedAttempts >= MaxFailedAttempts)
                    {
                        _logger.LogWarning("Too many failed attempts for email: {Email}", email);
                        return (false, "Too many failed attempts. Please request a new code.", null, null, null, null);
                    }

                    if (pendingByEmail.ExpiresAt < DateTime.UtcNow)
                    {
                        _logger.LogWarning("Expired signin code for email: {Email}", email);
                        return (false, "Your sign-in code has expired. Please request a new one", null, null, null, null);
                    }

                    // Increment failed attempts
                    pendingByEmail.FailedAttempts++;
                    await _pendingSignupRepository.UpdateAsync(pendingByEmail);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    _logger.LogWarning("Invalid code for email: {Email}", email);
                    return (false, "Invalid sign-in code", null, null, null, null);
                }

                _logger.LogWarning("Invalid or expired signin code for email: {Email}", email);
                return (false, "Invalid or expired sign-in code", null, null, null, null);
            }

            // Validate rate limiting
            if (pendingSignin.FailedAttempts >= MaxFailedAttempts)
            {
                _logger.LogWarning("Too many failed attempts for email: {Email}", email);
                return (false, "Too many failed attempts. Please request a new code.", null, null, null, null);
            }

            // Validate expiration
            if (pendingSignin.ExpiresAt < DateTime.UtcNow)
            {
                _logger.LogWarning("Expired signin code for email: {Email}", email);
                return (false, "Your sign-in code has expired. Please request a new one", null, null, null, null);
            }

            // Get account
            var account = await _accountRepository.GetByEmailAsync(email);
            if (account == null)
            {
                _logger.LogError("Account not found for email: {Email}", email);
                return (false, "Account not found", null, null, null, null);
            }

            // Update last login timestamp
            account.LastLoginAt = DateTime.UtcNow;
            await _accountRepository.UpdateAsync(account);

            // Mark pending signin as used
            pendingSignin.IsUsed = true;
            await _pendingSignupRepository.UpdateAsync(pendingSignin);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Signin verified for email: {Email}", email);

            // Generate JWT tokens
            var accessToken = _jwtService.GenerateAccessToken(
                account.Auth0UserId,
                account.Email,
                account.DisplayName,
                account.Role);
            var refreshToken = _jwtService.GenerateRefreshToken();

            return (true, "Sign-in successful", account, accessToken, refreshToken, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying signin for email: {Email}", command.Email);
            var errorDetails = $"{ex.GetType().Name}: {ex.Message}";
            if (ex.InnerException != null)
            {
                errorDetails += $" | Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}";
            }
            return (false, "An error occurred while verifying your sign-in", null, null, null, errorDetails);
        }
    }
}
