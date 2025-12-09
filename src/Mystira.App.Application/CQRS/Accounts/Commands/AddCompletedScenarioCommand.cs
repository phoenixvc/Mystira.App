namespace Mystira.App.Application.CQRS.Accounts.Commands;

/// <summary>
/// Command to add a completed scenario to an account.
/// Tracks which scenarios the account has finished.
/// </summary>
public record AddCompletedScenarioCommand(
    string AccountId,
    string ScenarioId
) : ICommand<bool>;
