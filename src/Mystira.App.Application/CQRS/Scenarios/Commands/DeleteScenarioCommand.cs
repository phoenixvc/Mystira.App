namespace Mystira.App.Application.CQRS.Scenarios.Commands;

/// <summary>
/// Command to delete a scenario (write operation)
/// </summary>
public record DeleteScenarioCommand(string ScenarioId) : ICommand;
