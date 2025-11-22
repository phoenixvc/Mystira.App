using Microsoft.Extensions.Logging;
using Mystira.App.Domain.Models;
using Mystira.App.Infrastructure.Data.Repositories;

namespace Mystira.App.Application.UseCases.Accounts;

/// <summary>
/// Use case for retrieving an account by email address
/// </summary>
public class GetAccountByEmailUseCase
{
    private readonly IAccountRepository _repository;
    private readonly ILogger<GetAccountByEmailUseCase> _logger;

    public GetAccountByEmailUseCase(
        IAccountRepository repository,
        ILogger<GetAccountByEmailUseCase> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Account?> ExecuteAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email cannot be null or empty", nameof(email));
        }

        var account = await _repository.GetByEmailAsync(email);

        if (account == null)
        {
            _logger.LogWarning("Account not found for email: {Email}", email);
        }
        else
        {
            _logger.LogDebug("Retrieved account by email: {Email}", email);
        }

        return account;
    }
}

