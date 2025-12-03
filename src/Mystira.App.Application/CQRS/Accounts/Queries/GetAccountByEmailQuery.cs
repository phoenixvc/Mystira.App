using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.Accounts.Queries;

public record GetAccountByEmailQuery(string Email) : IQuery<Account?>;
