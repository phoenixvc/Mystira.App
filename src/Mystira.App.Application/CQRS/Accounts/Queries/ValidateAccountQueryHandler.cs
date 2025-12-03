using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;

namespace Mystira.App.Application.CQRS.Accounts.Queries;

/// <summary>
/// Handler for validating account existence by email.
/// Returns true if account exists, false otherwise.
/// </summary>
public class ValidateAccountQueryHandler : IQueryHandler<ValidateAccountQuery, bool>
{
    private readonly IAccountRepository _repository;
    private readonly ILogger<ValidateAccountQueryHandler> _logger;

    public ValidateAccountQueryHandler(
        IAccountRepository repository,
        ILogger<ValidateAccountQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<bool> Handle(ValidateAccountQuery query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query.Email))
        {
            _logger.LogWarning("Cannot validate account: Email is null or empty");
            return false;
        }

        try
        {
            var account = await _repository.GetByEmailAsync(query.Email);
            var isValid = account != null;

            _logger.LogInformation("Account validation for {Email}: {IsValid}", query.Email, isValid);
            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating account for email {Email}", query.Email);
            return false;
        }
    }
}
