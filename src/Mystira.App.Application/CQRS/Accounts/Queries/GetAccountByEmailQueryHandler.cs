using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.Accounts.Queries;

/// <summary>
/// Wolverine handler for GetAccountByEmailQuery.
/// Uses static method convention for cleaner, more testable code.
/// </summary>
public static class GetAccountByEmailQueryHandler
{
    /// <summary>
    /// Handles the GetAccountByEmailQuery by retrieving an account from the repository by email.
    /// Wolverine injects dependencies as method parameters.
    /// </summary>
    public static async Task<Account?> Handle(
        GetAccountByEmailQuery query,
        IAccountRepository repository,
        ILogger logger,
        CancellationToken ct)
    {
        var account = await repository.GetByEmailAsync(query.Email);
        logger.LogDebug("Retrieved account by email {Email}", query.Email);
        return account;
    }
}
