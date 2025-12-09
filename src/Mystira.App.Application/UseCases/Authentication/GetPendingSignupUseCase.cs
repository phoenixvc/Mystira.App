using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.UseCases.Authentication;

/// <summary>
/// Use case for retrieving a pending signup by email and code
/// </summary>
public class GetPendingSignupUseCase
{
    private readonly IPendingSignupRepository _repository;
    private readonly ILogger<GetPendingSignupUseCase> _logger;

    public GetPendingSignupUseCase(
        IPendingSignupRepository repository,
        ILogger<GetPendingSignupUseCase> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<PendingSignup?> ExecuteAsync(string email, string code)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email cannot be null or empty", nameof(email));
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Code cannot be null or empty", nameof(code));
        }

        var normalizedEmail = email.ToLowerInvariant().Trim();
        var normalizedCode = code.Trim();

        var pendingSignup = await _repository.GetByEmailAndCodeAsync(normalizedEmail, normalizedCode);

        if (pendingSignup == null)
        {
            _logger.LogWarning("Pending signup not found for email: {Email}", normalizedEmail);
        }
        else
        {
            _logger.LogDebug("Retrieved pending signup for email: {Email}", normalizedEmail);
        }

        return pendingSignup;
    }
}

