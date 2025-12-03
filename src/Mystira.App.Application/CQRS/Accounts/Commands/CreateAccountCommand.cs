using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.Accounts.Commands;

public record CreateAccountCommand(
    string Auth0UserId,
    string Email,
    string? DisplayName,
    List<string>? UserProfileIds,
    SubscriptionDetails? Subscription,
    AccountSettings? Settings
) : ICommand<Account>;
