using Mystira.App.Application.Interfaces;

namespace Mystira.App.Application.CQRS.Accounts.Queries;

/// <summary>
/// Query to validate that an account exists and is accessible.
/// Typically used for account existence checks during authentication or authorization.
/// </summary>
public record ValidateAccountQuery(string Email) : IQuery<bool>;
