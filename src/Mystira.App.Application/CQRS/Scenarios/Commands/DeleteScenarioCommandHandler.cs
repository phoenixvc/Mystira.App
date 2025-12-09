using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;

namespace Mystira.App.Application.CQRS.Scenarios.Commands;

/// <summary>
/// Handler for DeleteScenarioCommand - deletes a scenario
/// This is a write operation that modifies state
/// </summary>
public class DeleteScenarioCommandHandler : ICommandHandler<DeleteScenarioCommand>
{
    private readonly IScenarioRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteScenarioCommandHandler> _logger;

    public DeleteScenarioCommandHandler(
        IScenarioRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<DeleteScenarioCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(DeleteScenarioCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.ScenarioId))
        {
            throw new ArgumentException("Scenario ID cannot be null or empty", nameof(command.ScenarioId));
        }

        var scenario = await _repository.GetByIdAsync(command.ScenarioId);

        if (scenario == null)
        {
            _logger.LogWarning("Scenario not found: {ScenarioId}", command.ScenarioId);
            throw new InvalidOperationException($"Scenario not found: {command.ScenarioId}");
        }

        await _repository.DeleteAsync(command.ScenarioId);

        try
        {
            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error deleting scenario: {ScenarioId}", command.ScenarioId);
            throw;
        }

        _logger.LogInformation("Deleted scenario: {ScenarioId}", command.ScenarioId);
    }
}
