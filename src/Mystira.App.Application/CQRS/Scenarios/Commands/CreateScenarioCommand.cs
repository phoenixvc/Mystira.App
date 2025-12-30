using Mystira.Contracts.App.Requests.Scenarios;
using Mystira.Domain.Models;

namespace Mystira.App.Application.CQRS.Scenarios.Commands;

/// <summary>
/// Command to create a new scenario (write operation)
/// </summary>
public record CreateScenarioCommand(CreateScenarioRequest Request) : ICommand<Scenario>;
