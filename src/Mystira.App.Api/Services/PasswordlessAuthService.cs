using System.Security.Cryptography;
using Mystira.App.Domain.Models;
using Mystira.App.Infrastructure.Data.Repositories;
using Mystira.App.Infrastructure.Data.UnitOfWork;

namespace Mystira.App.Api.Services;

public class PasswordlessAuthService : IPasswordlessAuthService
{
    private readonly IAccountRepository _accountRepository;
    private readonly IPendingSignupRepository _pendingSignupRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PasswordlessAuthService> _logger;
    private readonly IEmailService _emailService;
    private const int CodeExpiryMinutes = 15;
    private const int MaxFailedAttempts = 5;

    public PasswordlessAuthService(
        IAccountRepository accountRepository,
        IPendingSignupRepository pendingSignupRepository,
        IUnitOfWork unitOfWork,
        ILogger<PasswordlessAuthService> logger,
        IEmailService emailService)
    {
        _accountRepository = accountRepository;
        _pendingSignupRepository = pendingSignupRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _emailService = emailService;
    }

    public async Task<(bool Success, string Message, string? Code)> RequestSignupAsync(string email, string displayName)
    {
        try
        {
            email = email.ToLowerInvariant().Trim();

            var existingAccount = await _accountRepository.GetByEmailAsync(email);
            if (existingAccount != null)
            {
                _logger.LogWarning("Signup requested for existing email: {Email}", email);
                return (false, "An account with this email already exists", null);
            }

            var existingPending = await _pendingSignupRepository.GetActiveByEmailAsync(email);

            if (existingPending != null)
            {
                _logger.LogInformation("Signup already pending for email: {Email}, reusing existing code", email);
                return (true, "Check your email for the verification code", existingPending.Code);
            }

            var code = GenerateSecureCode();
            var pendingSignup = new PendingSignup
            {
                Email = email,
                DisplayName = displayName,
                Code = code,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(CodeExpiryMinutes),
                IsUsed = false,
                IsSignin = false
            };

            await _pendingSignupRepository.AddAsync(pendingSignup);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Signup requested for email: {Email} with display name: {DisplayName}", email, displayName);

            var (emailSuccess, emailError) = await _emailService.SendSignupCodeAsync(email, displayName, code);

            if (!emailSuccess)
            {
                _logger.LogWarning("Failed to send verification email to {Email}: {Error}", email, emailError);
                return (false, "Failed to send verification email. Please try again later.", null);
            }

            return (true, "Check your email for the verification code", code);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting signup for email: {Email}", email);
            return (false, "An error occurred while processing your signup", null);
        }
    }

    public async Task<(bool Success, string Message, Account? Account)> VerifySignupAsync(string email, string code)
    {
        try
        {
            email = email.ToLowerInvariant().Trim();
            code = code.Trim();

            var pendingSignup = await _pendingSignupRepository.GetByEmailAndCodeAsync(email, code);
            if (pendingSignup == null)
            {
                // Try to get by email only to check if it exists but code is wrong
                var pendingByEmail = await _pendingSignupRepository.GetActiveByEmailAsync(email);
                if (pendingByEmail != null && !pendingByEmail.IsUsed)
                {
                    if (pendingByEmail.FailedAttempts >= MaxFailedAttempts)
                    {
                        _logger.LogWarning("Too many failed attempts for email: {Email}", email);
                        return (false, "Too many failed attempts. Please request a new code.", null);
                    }

                    if (pendingByEmail.ExpiresAt < DateTime.UtcNow)
                    {
                        _logger.LogWarning("Expired code for email: {Email}", email);
                        return (false, "Your verification code has expired. Please request a new one", null);
                    }

                    pendingByEmail.FailedAttempts++;
                    await _pendingSignupRepository.UpdateAsync(pendingByEmail);
                    await _unitOfWork.SaveChangesAsync();
                    _logger.LogWarning("Invalid code for email: {Email}", email);
                    return (false, "Invalid verification code", null);
                }

                _logger.LogWarning("No pending signup found for email: {Email}", email);
                return (false, "Invalid or expired verification code", null);
            }

            if (pendingSignup.FailedAttempts >= MaxFailedAttempts)
            {
                _logger.LogWarning("Too many failed attempts for email: {Email}", email);
                return (false, "Too many failed attempts. Please request a new code.", null);
            }

            if (pendingSignup.ExpiresAt < DateTime.UtcNow)
            {
                _logger.LogWarning("Expired code for email: {Email}", email);
                return (false, "Your verification code has expired. Please request a new one", null);
            }

            var account = new Account
            {
                Auth0UserId = $"auth0|{Guid.NewGuid():N}",
                Email = email,
                DisplayName = pendingSignup.DisplayName,
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow
            };

            await _accountRepository.AddAsync(account);

            pendingSignup.IsUsed = true;
            await _pendingSignupRepository.UpdateAsync(pendingSignup);

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Signup verified for email: {Email}", email);

            return (true, "Account created successfully", account);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying signup for email: {Email}", email);
            return (false, "An error occurred while verifying your account", null);
        }
    }

    public async Task<bool> CleanupExpiredSignupsAsync()
    {
        try
        {
            var expiredSignups = (await _pendingSignupRepository.GetExpiredAsync()).ToList();

            if (expiredSignups.Any())
            {
                foreach (var signup in expiredSignups)
                {
                    await _pendingSignupRepository.DeleteAsync(signup.Id);
                }
                await _unitOfWork.SaveChangesAsync();
                _logger.LogInformation("Cleaned up {Count} expired pending signups", expiredSignups.Count);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up expired signups");
            return false;
        }
    }

    public async Task<(bool Success, string Message, string? Code)> RequestSigninAsync(string email)
    {
        try
        {
            email = email.ToLowerInvariant().Trim();

            var existingAccount = await _accountRepository.GetByEmailAsync(email);
            if (existingAccount == null)
            {
                _logger.LogWarning("Signin requested for non-existent email: {Email}", email);
                return (false, "No account found with this email. Please sign up first.", null);
            }

            var existingPending = await _pendingSignupRepository.GetActiveByEmailAsync(email);
            if (existingPending != null && existingPending.IsSignin)
            {
                _logger.LogInformation("Signin already pending for email: {Email}, reusing existing code", email);
                return (true, "Check your email for the sign-in code", existingPending.Code);
            }

            var code = GenerateSecureCode();
            var pendingSignin = new PendingSignup
            {
                Email = email,
                DisplayName = existingAccount.DisplayName,
                Code = code,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(CodeExpiryMinutes),
                IsUsed = false,
                IsSignin = true
            };

            await _pendingSignupRepository.AddAsync(pendingSignin);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Signin requested for email: {Email} with display name: {DisplayName}", email, existingAccount.DisplayName);

            var (emailSuccess, emailError) = await _emailService.SendSigninCodeAsync(email, existingAccount.DisplayName, code);

            if (!emailSuccess)
            {
                _logger.LogWarning("Failed to send sign-in email to {Email}: {Error}", email, emailError);
                return (false, "Failed to send sign-in email. Please try again later.", null);
            }

            return (true, "Check your email for the sign-in code", code);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting signin for email: {Email}", email);
            return (false, "An error occurred while processing your sign-in request", null);
        }
    }

    public async Task<(bool Success, string Message, Account? Account)> VerifySigninAsync(string email, string code)
    {
        try
        {
            email = email.ToLowerInvariant().Trim();
            code = code.Trim();

            var pendingSignin = await _pendingSignupRepository.GetByEmailAndCodeAsync(email, code);
            if (pendingSignin == null || !pendingSignin.IsSignin)
            {
                // Try to get by email only to check if it exists but code is wrong
                var pendingByEmail = await _pendingSignupRepository.GetActiveByEmailAsync(email);
                if (pendingByEmail != null && pendingByEmail.IsSignin && !pendingByEmail.IsUsed)
                {
                    if (pendingByEmail.FailedAttempts >= MaxFailedAttempts)
                    {
                        _logger.LogWarning("Too many failed attempts for email: {Email}", email);
                        return (false, "Too many failed attempts. Please request a new code.", null);
                    }

                    if (pendingByEmail.ExpiresAt < DateTime.UtcNow)
                    {
                        _logger.LogWarning("Expired signin code for email: {Email}", email);
                        return (false, "Your sign-in code has expired. Please request a new one", null);
                    }

                    pendingByEmail.FailedAttempts++;
                    await _pendingSignupRepository.UpdateAsync(pendingByEmail);
                    await _unitOfWork.SaveChangesAsync();
                    _logger.LogWarning("Invalid code for email: {Email}", email);
                    return (false, "Invalid sign-in code", null);
                }

                _logger.LogWarning("Invalid or expired signin code for email: {Email}", email);
                return (false, "Invalid or expired sign-in code", null);
            }

            if (pendingSignin.FailedAttempts >= MaxFailedAttempts)
            {
                _logger.LogWarning("Too many failed attempts for email: {Email}", email);
                return (false, "Too many failed attempts. Please request a new code.", null);
            }

            if (pendingSignin.ExpiresAt < DateTime.UtcNow)
            {
                _logger.LogWarning("Expired signin code for email: {Email}", email);
                return (false, "Your sign-in code has expired. Please request a new one", null);
            }

            var account = await _accountRepository.GetByEmailAsync(email);
            if (account == null)
            {
                _logger.LogError("Account not found for email: {Email}", email);
                return (false, "Account not found", null);
            }

            account.LastLoginAt = DateTime.UtcNow;
            await _accountRepository.UpdateAsync(account);

            pendingSignin.IsUsed = true;
            await _pendingSignupRepository.UpdateAsync(pendingSignin);

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Signin verified for email: {Email}", email);

            return (true, "Sign-in successful", account);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying signin for email: {Email}", email);
            return (false, "An error occurred while verifying your sign-in", null);
        }
    }

    public async Task<Account?> GetAccountByUserIdAsync(string userId)
    {
        try
        {
            return await _accountRepository.GetByAuth0UserIdAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting account by user ID: {UserId}", userId);
            return null;
        }
    }

    private string GenerateSecureCode()
    {
        return RandomNumberGenerator.GetInt32(100000, 999999).ToString("D6");
    }
}
