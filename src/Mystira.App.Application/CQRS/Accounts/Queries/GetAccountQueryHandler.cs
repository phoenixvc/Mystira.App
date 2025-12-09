using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.Accounts.Queries;

public class GetAccountQueryHandler : IQueryHandler<GetAccountQuery, Account?>
{
    private readonly IAccountRepository _repository;
    private readonly ILogger<GetAccountQueryHandler> _logger;

    public GetAccountQueryHandler(
        IAccountRepository repository,
        ILogger<GetAccountQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Account?> Handle(GetAccountQuery request, CancellationToken cancellationToken)
    {
        var account = await _repository.GetByIdAsync(request.AccountId);
        _logger.LogDebug("Retrieved account {AccountId}", request.AccountId);
        return account;
    }
}
