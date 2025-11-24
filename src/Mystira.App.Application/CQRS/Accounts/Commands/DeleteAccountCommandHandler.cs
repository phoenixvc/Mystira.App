using Microsoft.Extensions.Logging;
using Mystira.App.Application.Interfaces;
using Mystira.App.Application.Ports.Data;

namespace Mystira.App.Application.CQRS.Accounts.Commands;

public class DeleteAccountCommandHandler : ICommandHandler<DeleteAccountCommand, bool>
{
    private readonly IAccountRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteAccountCommandHandler> _logger;

    public DeleteAccountCommandHandler(
        IAccountRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<DeleteAccountCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> Handle(DeleteAccountCommand command, CancellationToken cancellationToken)
    {
        var account = await _repository.GetByIdAsync(command.AccountId);
        if (account == null)
        {
            _logger.LogWarning("Account not found: {AccountId}", command.AccountId);
            return false;
        }

        await _repository.DeleteAsync(account.Id);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted account {AccountId}", command.AccountId);
        return true;
    }
}
