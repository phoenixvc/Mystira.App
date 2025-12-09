using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.UseCases.ContentBundles;

/// <summary>
/// Use case for adding a scenario to a content bundle
/// </summary>
public class AddScenarioToBundleUseCase
{
    private readonly IContentBundleRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AddScenarioToBundleUseCase> _logger;

    public AddScenarioToBundleUseCase(
        IContentBundleRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<AddScenarioToBundleUseCase> logger)
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

        if (bundle.ScenarioIds == null)
        {
            bundle.ScenarioIds = new List<string>();
        }

        if (!bundle.ScenarioIds.Contains(scenarioId))
        {
            bundle.ScenarioIds.Add(scenarioId);
            await _repository.UpdateAsync(bundle);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Added scenario {ScenarioId} to bundle {BundleId}", scenarioId, bundleId);
        }
        else
        {
            _logger.LogDebug("Scenario {ScenarioId} already exists in bundle {BundleId}", scenarioId, bundleId);
        }

        return bundle;
    }
}

