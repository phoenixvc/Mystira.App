using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Contracts.Requests.Auth;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.UseCases.Authentication;

/// <summary>
/// Use case for validating a pending signup code
/// </summary>
public class ValidatePendingSignupUseCase
{
    private readonly IPendingSignupRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ValidatePendingSignupUseCase> _logger;
    private const int MaxFailedAttempts = 5;

    public ValidatePendingSignupUseCase(
        IPendingSignupRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<ValidatePendingSignupUseCase> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<(bool IsValid, string Message, PendingSignup? PendingSignup)> ExecuteAsync(PasswordlessVerifyRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var email = request.Email.ToLowerInvariant().Trim();
        var code = request.Code.Trim();

        var pendingSignup = await _repository.GetByEmailAndCodeAsync(email, code);

        if (pendingSignup == null)
        {
            // Check if there's a pending signup with wrong code
            var pendingByEmail = await _repository.GetActiveByEmailAsync(email);
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

                // Increment failed attempts
                pendingByEmail.FailedAttempts++;
                await _repository.UpdateAsync(pendingByEmail);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogWarning("Invalid code for email: {Email}, failed attempts: {FailedAttempts}",
                    email, pendingByEmail.FailedAttempts);
                return (false, "Invalid verification code", null);
            }

            _logger.LogWarning("No pending signup found for email: {Email}", email);
            return (false, "Invalid or expired verification code", null);
        }

        // Validate the found pending signup
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

        if (pendingSignup.IsUsed)
        {
            _logger.LogWarning("Code already used for email: {Email}", email);
            return (false, "This verification code has already been used", null);
        }

        _logger.LogInformation("Validated pending signup for email: {Email}", email);
        return (true, "Code is valid", pendingSignup);
    }
}

