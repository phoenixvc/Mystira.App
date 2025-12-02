using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Auth;
using Mystira.App.Application.Interfaces;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.Auth.Commands;

/// <summary>
/// Handler for verifying passwordless signup code and creating account.
/// Validates code with rate limiting, creates account, and generates JWT tokens.
/// </summary>
public class VerifyPasswordlessSignupCommandHandler
    : ICommandHandler<VerifyPasswordlessSignupCommand, (bool Success, string Message, Account? Account, string? AccessToken, string? RefreshToken, string? ErrorDetails)>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IPendingSignupRepository _pendingSignupRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtService _jwtService;
    private readonly ILogger<VerifyPasswordlessSignupCommandHandler> _logger;
    private const int MaxFailedAttempts = 5;

    public VerifyPasswordlessSignupCommandHandler(
        IAccountRepository accountRepository,
        IPendingSignupRepository pendingSignupRepository,
        IUnitOfWork unitOfWork,
        IJwtService jwtService,
        ILogger<VerifyPasswordlessSignupCommandHandler> logger)
    {
        _accountRepository = accountRepository;
        _pendingSignupRepository = pendingSignupRepository;
        _unitOfWork = unitOfWork;
        _jwtService = jwtService;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, Account? Account, string? AccessToken, string? RefreshToken, string? ErrorDetails)> Handle(
        VerifyPasswordlessSignupCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var email = command.Email.ToLowerInvariant().Trim();
            var code = command.Code.Trim();

            // Try to find pending signup with matching email and code
            var pendingSignup = await _pendingSignupRepository.GetByEmailAndCodeAsync(email, code);
            if (pendingSignup == null)
            {
                // Check if there's a pending signup for this email (code might be wrong)
                var pendingByEmail = await _pendingSignupRepository.GetActiveByEmailAsync(email);
                if (pendingByEmail != null && !pendingByEmail.IsUsed)
                {
                    if (pendingByEmail.FailedAttempts >= MaxFailedAttempts)
                    {
                        _logger.LogWarning("Too many failed attempts for email: {Email}", email);
                        return (false, "Too many failed attempts. Please request a new code.", null, null, null, null);
                    }

                    if (pendingByEmail.ExpiresAt < DateTime.UtcNow)
                    {
                        _logger.LogWarning("Expired code for email: {Email}", email);
                        return (false, "Your verification code has expired. Please request a new one", null, null, null, null);
                    }

                    // Increment failed attempts
                    pendingByEmail.FailedAttempts++;
                    await _pendingSignupRepository.UpdateAsync(pendingByEmail);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    _logger.LogWarning("Invalid code for email: {Email}", email);
                    return (false, "Invalid verification code", null, null, null, null);
                }

                _logger.LogWarning("No pending signup found for email: {Email}", email);
                return (false, "Invalid or expired verification code", null, null, null, null);
            }

            // Validate rate limiting
            if (pendingSignup.FailedAttempts >= MaxFailedAttempts)
            {
                _logger.LogWarning("Too many failed attempts for email: {Email}", email);
                return (false, "Too many failed attempts. Please request a new code.", null, null, null, null);
            }

            // Validate expiration
            if (pendingSignup.ExpiresAt < DateTime.UtcNow)
            {
                _logger.LogWarning("Expired code for email: {Email}", email);
                return (false, "Your verification code has expired. Please request a new one", null, null, null, null);
            }

            // Create new account
            var account = new Account
            {
                Auth0UserId = $"auth0|{Guid.NewGuid():N}",
                Email = email,
                DisplayName = pendingSignup.DisplayName,
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow
            };

            await _accountRepository.AddAsync(account);

            // Mark pending signup as used
            pendingSignup.IsUsed = true;
            await _pendingSignupRepository.UpdateAsync(pendingSignup);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Signup verified for email: {Email}", email);

            // Generate JWT tokens
            var accessToken = _jwtService.GenerateAccessToken(
                account.Auth0UserId,
                account.Email,
                account.DisplayName,
                account.Role);
            var refreshToken = _jwtService.GenerateRefreshToken();

            return (true, "Account created successfully", account, accessToken, refreshToken, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying signup for email: {Email}", command.Email);
            var errorDetails = ExceptionDetailsHelper.FormatExceptionDetails(ex);
            return (false, "An error occurred while verifying your account", null, null, null, errorDetails);
        }
    }
}
