using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.Contracts.App.Requests.Accounts;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.UseCases.Accounts;

/// <summary>
/// Use case for creating a new account
/// </summary>
public class CreateAccountUseCase
{
    private readonly IAccountRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateAccountUseCase> _logger;

    public CreateAccountUseCase(
        IAccountRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<CreateAccountUseCase> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Account> ExecuteAsync(CreateAccountRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        // Check if account with email already exists
        var existingAccount = await _repository.GetByEmailAsync(request.Email);
        if (existingAccount != null)
        {
            throw new InvalidOperationException($"Account with email {request.Email} already exists");
        }

        var account = new Account
        {
            Id = Guid.NewGuid().ToString(),
            Auth0UserId = request.Auth0UserId,
            Email = request.Email,
            DisplayName = request.DisplayName,
            Role = "Guest",
            UserProfileIds = new List<string>(),
            CompletedScenarioIds = new List<string>(),
            Subscription = new SubscriptionDetails(),
            Settings = new AccountSettings(),
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow
        };

        await _repository.AddAsync(account);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Created new account: {AccountId} for {Email}", account.Id, account.Email);
        return account;
    }
}

