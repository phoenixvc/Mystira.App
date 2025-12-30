using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.Domain.Models;

namespace Mystira.App.Application.CQRS.Accounts.Queries;

/// <summary>
/// Wolverine handler for GetAccountQuery.
/// Uses static method convention for cleaner, more testable code.
/// </summary>
public static class GetAccountQueryHandler
{
    /// <summary>
    /// Handles the GetAccountQuery by retrieving an account from the repository.
    /// Wolverine injects dependencies as method parameters.
    /// </summary>
    public static async Task<Account?> Handle(
        GetAccountQuery query,
        IAccountRepository repository,
        ILogger logger,
        CancellationToken ct)
    {
        var account = await repository.GetByIdAsync(query.AccountId);
        logger.LogDebug("Retrieved account {AccountId}", query.AccountId);
        return account;
    }
}
