using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Mystira.App.Api.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Api.Services;

public class PasswordlessAuthService : IPasswordlessAuthService
{
    private readonly MystiraAppDbContext _context;
    private readonly ILogger<PasswordlessAuthService> _logger;
    private readonly IEmailService _emailService;
    private const int CodeExpiryMinutes = 15;
    private const int MaxFailedAttempts = 5;

    public PasswordlessAuthService(MystiraAppDbContext context, ILogger<PasswordlessAuthService> logger, IEmailService emailService)
    {
        _context = context;
        _logger = logger;
        _emailService = emailService;
    }

    public async Task<(bool Success, string Message, string? Code)> RequestSignupAsync(string email, string displayName)
    {
        try
        {
            email = email.ToLowerInvariant().Trim();

            var existingAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.Email == email);
            if (existingAccount != null)
            {
                _logger.LogWarning("Signup requested for existing email: {Email}", email);
                return (false, "An account with this email already exists", null);
            }

            var existingPending = await _context.PendingSignups
                .FirstOrDefaultAsync(p => p.Email == email && !p.IsUsed && p.ExpiresAt > DateTime.UtcNow);

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

            _context.PendingSignups.Add(pendingSignup);
            await _context.SaveChangesAsync();

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

            var pendingSignup = await _context.PendingSignups
                .FirstOrDefaultAsync(p => p.Email == email && !p.IsUsed);

            if (pendingSignup == null)
            {
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

            if (pendingSignup.Code != code)
            {
                pendingSignup.FailedAttempts++;
                await _context.SaveChangesAsync();
                _logger.LogWarning("Invalid code for email: {Email}", email);
                return (false, "Invalid verification code", null);
            }

            var account = new Account
            {
                Auth0UserId = $"auth0|{Guid.NewGuid():N}",
                Email = email,
                DisplayName = pendingSignup.DisplayName,
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow
            };

            _context.Accounts.Add(account);

            pendingSignup.IsUsed = true;
            _context.PendingSignups.Update(pendingSignup);

            await _context.SaveChangesAsync();

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
            var expiredSignups = await _context.PendingSignups
                .Where(p => p.ExpiresAt < DateTime.UtcNow && !p.IsUsed)
                .ToListAsync();

            if (expiredSignups.Any())
            {
                _context.PendingSignups.RemoveRange(expiredSignups);
                await _context.SaveChangesAsync();
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

            var existingAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.Email == email);
            if (existingAccount == null)
            {
                _logger.LogWarning("Signin requested for non-existent email: {Email}", email);
                return (false, "No account found with this email. Please sign up first.", null);
            }

            var existingPending = await _context.PendingSignups
                .FirstOrDefaultAsync(p => p.Email == email && !p.IsUsed && p.ExpiresAt > DateTime.UtcNow && p.IsSignin);

            if (existingPending != null)
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

            _context.PendingSignups.Add(pendingSignin);
            await _context.SaveChangesAsync();

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

            var pendingSignin = await _context.PendingSignups
                .FirstOrDefaultAsync(p => p.Email == email && !p.IsUsed && p.IsSignin);

            if (pendingSignin == null)
            {
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

            if (pendingSignin.Code != code)
            {
                pendingSignin.FailedAttempts++;
                await _context.SaveChangesAsync();
                _logger.LogWarning("Invalid code for email: {Email}", email);
                return (false, "Invalid sign-in code", null);
            }

            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Email == email);
            if (account == null)
            {
                _logger.LogError("Account not found for email: {Email}", email);
                return (false, "Account not found", null);
            }

            account.LastLoginAt = DateTime.UtcNow;
            _context.Accounts.Update(account);

            pendingSignin.IsUsed = true;
            _context.PendingSignups.Update(pendingSignin);

            await _context.SaveChangesAsync();

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
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Auth0UserId == userId);

            return account;
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
