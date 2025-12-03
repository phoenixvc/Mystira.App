namespace Mystira.App.Application.CQRS.Accounts.Commands;

public record DeleteAccountCommand(string AccountId) : ICommand<bool>;
