using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Auth;
using Mystira.App.Application.Interfaces;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.Auth.Commands;

/// <summary>
/// Handler for requesting a passwordless signin verification code.
/// Validates account exists, generates secure code, and sends signin email.
/// </summary>
public class RequestPasswordlessSigninCommandHandler
    : ICommandHandler<RequestPasswordlessSigninCommand, (bool Success, string Message, string? Code, string? ErrorDetails)>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IPendingSignupRepository _pendingSignupRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly ILogger<RequestPasswordlessSigninCommandHandler> _logger;
    private const int CodeExpiryMinutes = 15;

    public RequestPasswordlessSigninCommandHandler(
        IAccountRepository accountRepository,
        IPendingSignupRepository pendingSignupRepository,
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        ILogger<RequestPasswordlessSigninCommandHandler> logger)
    {
        _accountRepository = accountRepository;
        _pendingSignupRepository = pendingSignupRepository;
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, string? Code, string? ErrorDetails)> Handle(
        RequestPasswordlessSigninCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var email = command.Email.ToLowerInvariant().Trim();

            // Check if account exists
            var existingAccount = await _accountRepository.GetByEmailAsync(email);
            if (existingAccount == null)
            {
                _logger.LogWarning("Signin requested for non-existent email: {Email}", email);
                return (false, "No account found with this email. Please sign up first.", null, null);
            }

            // Check if there's already a pending signin
            var existingPending = await _pendingSignupRepository.GetActiveByEmailAsync(email);
            if (existingPending != null && existingPending.IsSignin)
            {
                _logger.LogInformation("Signin already pending for email: {Email}, reusing existing code", email);
                return (true, "Check your email for the sign-in code", existingPending.Code, null);
            }

            // Generate secure verification code
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
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Signin requested for email: {Email} with display name: {DisplayName}",
                email, existingAccount.DisplayName);

            // Send signin email
            var (emailSuccess, emailError) = await _emailService.SendSigninCodeAsync(
                email,
                existingAccount.DisplayName,
                code);

            if (!emailSuccess)
            {
                _logger.LogWarning("Failed to send sign-in email to {Email}: {Error}", email, emailError);
                return (false, "Failed to send sign-in email. Please try again later.", null, emailError);
            }

            return (true, "Check your email for the sign-in code", code, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting signin for email: {Email}", command.Email);
            var errorDetails = $"{ex.GetType().Name}: {ex.Message}";
            if (ex.InnerException != null)
            {
                errorDetails += $" | Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}";
            }
            return (false, "An error occurred while processing your sign-in request", null, errorDetails);
        }
    }

    private static string GenerateSecureCode()
    {
        return RandomNumberGenerator.GetInt32(100000, 999999).ToString("D6");
    }
}
