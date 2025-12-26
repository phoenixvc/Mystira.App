using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;

namespace Mystira.App.Application.CQRS.Scenarios.Commands;

/// <summary>
/// Wolverine handler for DeleteScenarioCommand.
/// Deletes a scenario - this is a write operation that modifies state.
/// </summary>
public static class DeleteScenarioCommandHandler
{
    /// <summary>
    /// Handles the DeleteScenarioCommand by deleting a scenario from the repository.
    /// Wolverine injects dependencies as method parameters.
    /// </summary>
    public static async Task Handle(
        DeleteScenarioCommand command,
        IScenarioRepository repository,
        IUnitOfWork unitOfWork,
        ILogger logger,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.ScenarioId))
        {
            throw new ArgumentException("Scenario ID cannot be null or empty", nameof(command.ScenarioId));
        }

        var scenario = await repository.GetByIdAsync(command.ScenarioId);

        if (scenario == null)
        {
            logger.LogWarning("Scenario not found: {ScenarioId}", command.ScenarioId);
            throw new InvalidOperationException($"Scenario not found: {command.ScenarioId}");
        }

        await repository.DeleteAsync(command.ScenarioId);

        try
        {
            await unitOfWork.SaveChangesAsync();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error deleting scenario: {ScenarioId}", command.ScenarioId);
            throw;
        }

        logger.LogInformation("Deleted scenario: {ScenarioId}", command.ScenarioId);
    }
}
