using Microsoft.Extensions.Logging;
using Mystira.App.Domain.Models;
using Mystira.App.Application.Ports.Data;

namespace Mystira.App.Application.UseCases.ContentBundles;

/// <summary>
/// Use case for updating a content bundle
/// </summary>
public class UpdateContentBundleUseCase
{
    private readonly IContentBundleRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateContentBundleUseCase> _logger;

    public UpdateContentBundleUseCase(
        IContentBundleRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateContentBundleUseCase> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ContentBundle> ExecuteAsync(
        string bundleId,
        string? title = null,
        string? description = null,
        List<string>? scenarioIds = null,
        string? imageId = null,
        List<BundlePrice>? prices = null,
        bool? isFree = null,
        string? ageGroup = null)
    {
        if (string.IsNullOrWhiteSpace(bundleId))
        {
            throw new ArgumentException("Bundle ID cannot be null or empty", nameof(bundleId));
        }

        var bundle = await _repository.GetByIdAsync(bundleId);
        if (bundle == null)
        {
            throw new ArgumentException($"Content bundle not found: {bundleId}", nameof(bundleId));
        }

        // Update properties if provided
        if (title != null)
        {
            bundle.Title = title;
        }

        if (description != null)
        {
            bundle.Description = description;
        }

        if (scenarioIds != null)
        {
            bundle.ScenarioIds = scenarioIds;
        }

        if (imageId != null)
        {
            bundle.ImageId = imageId;
        }

        if (prices != null)
        {
            bundle.Prices = prices;
        }

        if (isFree.HasValue)
        {
            bundle.IsFree = isFree.Value;
        }

        if (ageGroup != null)
        {
            bundle.AgeGroup = ageGroup;
        }

        await _repository.UpdateAsync(bundle);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Updated content bundle: {BundleId}", bundleId);
        return bundle;
    }
}

