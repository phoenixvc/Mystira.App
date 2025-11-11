using Mystira.App.Api.Data;
using Mystira.App.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Mystira.App.Api.Services;

public class PasswordlessAuthService : IPasswordlessAuthService
{
    private readonly MystiraAppDbContext _context;
    private readonly ILogger<PasswordlessAuthService> _logger;
    private readonly IEmailService _emailService;
    private const int CodeExpiryMinutes = 15;
    private const int CodeLength = 6;

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

            var code = GenerateCode();
            var pendingSignup = new PendingSignup
            {
                Email = email,
                DisplayName = displayName,
                Code = code,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(CodeExpiryMinutes),
                IsUsed = false
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
                .FirstOrDefaultAsync(p => p.Email == email && p.Code == code && !p.IsUsed);

            if (pendingSignup == null)
            {
                _logger.LogWarning("Invalid or expired code for email: {Email}", email);
                return (false, "Invalid or expired verification code", null);
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

    private string GenerateCode()
    {
        var random = new Random();
        return random.Next(100000, 999999).ToString();
    }
}
