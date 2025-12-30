using Mystira.Domain.Models;

namespace Mystira.App.Application.CQRS.Accounts.Queries;

public record GetAccountQuery(string AccountId) : IQuery<Account?>;
