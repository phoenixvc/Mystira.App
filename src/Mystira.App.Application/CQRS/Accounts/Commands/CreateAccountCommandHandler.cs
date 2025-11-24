using Microsoft.Extensions.Logging;
using Mystira.App.Application.Interfaces;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.Accounts.Commands;

public class CreateAccountCommandHandler : ICommandHandler<CreateAccountCommand, Account>
{
    private readonly IAccountRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateAccountCommandHandler> _logger;

    public CreateAccountCommandHandler(
        IAccountRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<CreateAccountCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Account> Handle(CreateAccountCommand command, CancellationToken cancellationToken)
    {
        // Check if account already exists
        var existing = await _repository.GetByEmailAsync(command.Email);
        if (existing != null)
            throw new InvalidOperationException($"Account with email {command.Email} already exists");

        var account = new Account
        {
            Id = Guid.NewGuid().ToString(),
            Auth0UserId = command.Auth0UserId,
            Email = command.Email,
            DisplayName = command.DisplayName ?? command.Email.Split('@')[0],
            UserProfileIds = command.UserProfileIds ?? new List<string>(),
            Subscription = command.Subscription ?? new SubscriptionDetails(),
            Settings = command.Settings ?? new AccountSettings(),
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow
        };

        await _repository.AddAsync(account);
        await _unitOfWork.CommitAsync(cancellationToken);

        _logger.LogInformation("Created account {AccountId} for email {Email}", account.Id, account.Email);
        return account;
    }
}
