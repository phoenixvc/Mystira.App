using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.UseCases.ContentBundles;

/// <summary>
/// Use case for removing a scenario from a content bundle
/// </summary>
public class RemoveScenarioFromBundleUseCase
{
    private readonly IContentBundleRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RemoveScenarioFromBundleUseCase> _logger;

    public RemoveScenarioFromBundleUseCase(
        IContentBundleRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<RemoveScenarioFromBundleUseCase> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ContentBundle> ExecuteAsync(string bundleId, string scenarioId)
    {
        if (string.IsNullOrWhiteSpace(bundleId))
        {
            throw new ArgumentException("Bundle ID cannot be null or empty", nameof(bundleId));
        }

        if (string.IsNullOrWhiteSpace(scenarioId))
        {
            throw new ArgumentException("Scenario ID cannot be null or empty", nameof(scenarioId));
        }

        var bundle = await _repository.GetByIdAsync(bundleId);
        if (bundle == null)
        {
            throw new ArgumentException($"Content bundle not found: {bundleId}", nameof(bundleId));
        }

        if (bundle.ScenarioIds != null && bundle.ScenarioIds.Contains(scenarioId))
        {
            bundle.ScenarioIds.Remove(scenarioId);
            await _repository.UpdateAsync(bundle);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Removed scenario {ScenarioId} from bundle {BundleId}", scenarioId, bundleId);
        }
        else
        {
            _logger.LogDebug("Scenario {ScenarioId} not found in bundle {BundleId}", scenarioId, bundleId);
        }

        return bundle;
    }
}

