using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Auth;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.Auth.Commands;

/// <summary>
/// Handler for requesting a passwordless signup verification code.
/// Validates email availability, generates secure code, and sends verification email.
/// </summary>
public class RequestPasswordlessSignupCommandHandler
    : ICommandHandler<RequestPasswordlessSignupCommand, (bool Success, string Message, string? Code, string? ErrorDetails)>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IPendingSignupRepository _pendingSignupRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly ILogger<RequestPasswordlessSignupCommandHandler> _logger;
    private const int CodeExpiryMinutes = 15;

    public RequestPasswordlessSignupCommandHandler(
        IAccountRepository accountRepository,
        IPendingSignupRepository pendingSignupRepository,
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        ILogger<RequestPasswordlessSignupCommandHandler> logger)
    {
        _accountRepository = accountRepository;
        _pendingSignupRepository = pendingSignupRepository;
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, string? Code, string? ErrorDetails)> Handle(
        RequestPasswordlessSignupCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var email = command.Email.ToLowerInvariant().Trim();
            var displayName = command.DisplayName;

            // Check if account already exists
            var existingAccount = await _accountRepository.GetByEmailAsync(email);
            if (existingAccount != null)
            {
                _logger.LogWarning("Signup requested for existing email: {Email}", email);
                return (false, "An account with this email already exists", null, null);
            }

            // BELOW BLOCKS RESEND CODE. TODO: Fix.
            // Check if there's already a pending signup
            // var existingPending = await _pendingSignupRepository.GetActiveByEmailAsync(email);
            // if (existingPending != null)
            // {
            //     _logger.LogInformation("Signup already pending for email: {Email}, reusing existing code", email);
            //     return (true, "Check your email for the verification code", existingPending.Code, null);
            // }

            // Generate secure verification code
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
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Signup requested for email: {Email} with display name: {DisplayName}",
                email, displayName);

            // Send verification email
            var (emailSuccess, emailError) = await _emailService.SendSignupCodeAsync(email, displayName, code);
            if (!emailSuccess)
            {
                _logger.LogWarning("Failed to send verification email to {Email}: {Error}", email, emailError);
                return (false, "Failed to send verification email. Please try again later.", null, emailError);
            }

            return (true, "Check your email for the verification code", code, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting signup for email: {Email}", command.Email);
            var errorDetails = $"{ex.GetType().Name}: {ex.Message}";
            if (ex.InnerException != null)
            {
                errorDetails += $" | Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}";
            }
            return (false, "An error occurred while processing your signup", null, errorDetails);
        }
    }

    private static string GenerateSecureCode()
    {
        return RandomNumberGenerator.GetInt32(100000, 999999).ToString("D6");
    }
}
