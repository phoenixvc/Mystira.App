using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Contracts.Requests.Auth;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.UseCases.Authentication;

/// <summary>
/// Use case for creating a passwordless signup request
/// </summary>
public class CreatePendingSignupUseCase
{
    private readonly IAccountRepository _accountRepository;
    private readonly IPendingSignupRepository _pendingSignupRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreatePendingSignupUseCase> _logger;
    private const int CodeExpiryMinutes = 15;

    public CreatePendingSignupUseCase(
        IAccountRepository accountRepository,
        IPendingSignupRepository pendingSignupRepository,
        IUnitOfWork unitOfWork,
        ILogger<CreatePendingSignupUseCase> logger)
    {
        _accountRepository = accountRepository;
        _pendingSignupRepository = pendingSignupRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<PendingSignup> ExecuteAsync(PasswordlessSignupRequest request, bool isSignin = false)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var email = request.Email.ToLowerInvariant().Trim();

        // Check if account already exists
        var existingAccount = await _accountRepository.GetByEmailAsync(email);
        if (existingAccount != null && !isSignin)
        {
            throw new InvalidOperationException($"Account with email {email} already exists");
        }

        // Check for existing pending signup
        var existingPending = await _pendingSignupRepository.GetActiveByEmailAsync(email);
        if (existingPending != null && !existingPending.IsUsed)
        {
            _logger.LogInformation("Reusing existing pending signup for email: {Email}", email);
            return existingPending;
        }

        var code = GenerateSecureCode();
        var pendingSignup = new PendingSignup
        {
            Email = email,
            DisplayName = request.DisplayName,
            Code = code,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(CodeExpiryMinutes),
            IsUsed = false,
            IsSignin = isSignin,
            FailedAttempts = 0
        };

        await _pendingSignupRepository.AddAsync(pendingSignup);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Created pending signup for email: {Email}, isSignin: {IsSignin}", email, isSignin);
        return pendingSignup;
    }

    private static string GenerateSecureCode()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[4];
        rng.GetBytes(bytes);
        var code = BitConverter.ToUInt32(bytes, 0) % 1000000;
        return code.ToString("D6");
    }
}

