using Mystira.Domain.Models;

namespace Mystira.App.Application.CQRS.Accounts.Commands;

public record CreateAccountCommand(
    string ExternalUserId,
    string Email,
    string? DisplayName,
    List<string>? UserProfileIds,
    SubscriptionDetails? Subscription,
    AccountSettings? Settings
) : ICommand<Account>;
