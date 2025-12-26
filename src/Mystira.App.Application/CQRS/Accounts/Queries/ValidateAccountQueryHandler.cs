using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;

namespace Mystira.App.Application.CQRS.Accounts.Queries;

/// <summary>
/// Wolverine handler for ValidateAccountQuery.
/// Validates account existence by email.
/// Returns true if account exists, false otherwise.
/// Uses static method convention for cleaner, more testable code.
/// </summary>
public static class ValidateAccountQueryHandler
{
    /// <summary>
    /// Handles the ValidateAccountQuery by checking if an account exists with the given email.
    /// Wolverine injects dependencies as method parameters.
    /// </summary>
    public static async Task<bool> Handle(
        ValidateAccountQuery query,
        IAccountRepository repository,
        ILogger logger,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query.Email))
        {
            logger.LogWarning("Cannot validate account: Email is null or empty");
            return false;
        }

        try
        {
            var account = await repository.GetByEmailAsync(query.Email);
            var isValid = account != null;

            logger.LogInformation("Account validation for {Email}: {IsValid}", query.Email, isValid);
            return isValid;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error validating account for email {Email}", query.Email);
            return false;
        }
    }
}
