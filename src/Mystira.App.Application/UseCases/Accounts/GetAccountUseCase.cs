using Microsoft.Extensions.Logging;
using Mystira.App.Domain.Models;
using Mystira.App.Infrastructure.Data.Repositories;

namespace Mystira.App.Application.UseCases.Accounts;

/// <summary>
/// Use case for retrieving an account by ID
/// </summary>
public class GetAccountUseCase
{
    private readonly IAccountRepository _repository;
    private readonly ILogger<GetAccountUseCase> _logger;

    public GetAccountUseCase(
        IAccountRepository repository,
        ILogger<GetAccountUseCase> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Account?> ExecuteAsync(string accountId)
    {
        if (string.IsNullOrWhiteSpace(accountId))
        {
            throw new ArgumentException("Account ID cannot be null or empty", nameof(accountId));
        }

        var account = await _repository.GetByIdAsync(accountId);

        if (account == null)
        {
            _logger.LogWarning("Account not found: {AccountId}", accountId);
        }
        else
        {
            _logger.LogDebug("Retrieved account: {AccountId}", accountId);
        }

        return account;
    }
}

