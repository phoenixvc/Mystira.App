using Microsoft.Extensions.Logging;
using Mystira.App.Domain.Models;
using Mystira.App.Infrastructure.Data.Repositories;
using Mystira.App.Infrastructure.Data.UnitOfWork;

namespace Mystira.App.Application.UseCases.Authentication;

/// <summary>
/// Use case for marking expired pending signups
/// </summary>
public class ExpirePendingSignupUseCase
{
    private readonly IPendingSignupRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ExpirePendingSignupUseCase> _logger;

    public ExpirePendingSignupUseCase(
        IPendingSignupRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<ExpirePendingSignupUseCase> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<int> ExecuteAsync()
    {
        var expiredSignups = await _repository.GetExpiredAsync();
        var expiredList = expiredSignups.Where(s => !s.IsUsed && s.ExpiresAt < DateTime.UtcNow).ToList();

        foreach (var signup in expiredList)
        {
            // Mark as used to prevent further use
            signup.IsUsed = true;
            await _repository.UpdateAsync(signup);
        }

        if (expiredList.Any())
        {
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Expired {Count} pending signups", expiredList.Count);
        }

        return expiredList.Count;
    }

    public async Task<bool> ExecuteAsync(string signupId)
    {
        if (string.IsNullOrWhiteSpace(signupId))
        {
            throw new ArgumentException("Signup ID cannot be null or empty", nameof(signupId));
        }

        var signup = await _repository.GetByIdAsync(signupId);
        if (signup == null)
        {
            _logger.LogWarning("Pending signup not found: {SignupId}", signupId);
            return false;
        }

        if (signup.IsUsed)
        {
            _logger.LogDebug("Pending signup already used: {SignupId}", signupId);
            return false;
        }

        signup.IsUsed = true;
        await _repository.UpdateAsync(signup);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Expired pending signup: {SignupId}", signupId);
        return true;
    }
}

