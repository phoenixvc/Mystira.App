using Microsoft.Extensions.Logging;
using Mystira.App.Infrastructure.Data.Repositories;
using Mystira.App.Infrastructure.Data.UnitOfWork;

namespace Mystira.App.Application.UseCases.Scenarios;

/// <summary>
/// Use case for deleting a scenario
/// </summary>
public class DeleteScenarioUseCase
{
    private readonly IScenarioRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteScenarioUseCase> _logger;

    public DeleteScenarioUseCase(
        IScenarioRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<DeleteScenarioUseCase> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> ExecuteAsync(string id)
    {
        var scenario = await _repository.GetByIdAsync(id);
        if (scenario == null)
        {
            return false;
        }

        await _repository.DeleteAsync(id);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Deleted scenario: {ScenarioId} - {Title}", scenario.Id, scenario.Title);
        return true;
    }
}

