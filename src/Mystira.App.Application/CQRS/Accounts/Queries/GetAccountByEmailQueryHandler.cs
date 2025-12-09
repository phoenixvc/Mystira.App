using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.Accounts.Queries;

public class GetAccountByEmailQueryHandler : IQueryHandler<GetAccountByEmailQuery, Account?>
{
    private readonly IAccountRepository _repository;
    private readonly ILogger<GetAccountByEmailQueryHandler> _logger;

    public GetAccountByEmailQueryHandler(
        IAccountRepository repository,
        ILogger<GetAccountByEmailQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Account?> Handle(GetAccountByEmailQuery request, CancellationToken cancellationToken)
    {
        var account = await _repository.GetByEmailAsync(request.Email);
        _logger.LogDebug("Retrieved account by email {Email}", request.Email);
        return account;
    }
}
