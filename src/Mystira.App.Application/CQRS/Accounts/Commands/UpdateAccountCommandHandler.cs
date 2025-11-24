using Microsoft.Extensions.Logging;
using Mystira.App.Application.Interfaces;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.Accounts.Commands;

public class UpdateAccountCommandHandler : ICommandHandler<UpdateAccountCommand, Account?>
{
    private readonly IAccountRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateAccountCommandHandler> _logger;

    public UpdateAccountCommandHandler(
        IAccountRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateAccountCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Account?> Handle(UpdateAccountCommand command, CancellationToken cancellationToken)
    {
        var account = await _repository.GetByIdAsync(command.AccountId);
        if (account == null)
        {
            _logger.LogWarning("Account not found: {AccountId}", command.AccountId);
            return null;
        }

        if (!string.IsNullOrEmpty(command.DisplayName))
            account.DisplayName = command.DisplayName;

        if (command.UserProfileIds != null)
            account.UserProfileIds = command.UserProfileIds;

        if (command.Subscription != null)
            account.Subscription = command.Subscription;

        if (command.Settings != null)
            account.Settings = command.Settings;

        await _repository.UpdateAsync(account);
        await _unitOfWork.CommitAsync(cancellationToken);

        _logger.LogInformation("Updated account {AccountId}", account.Id);
        return account;
    }
}
