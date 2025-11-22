using Microsoft.Extensions.Logging;
using Mystira.App.Domain.Models;
using Mystira.App.Infrastructure.Data.Repositories;

namespace Mystira.App.Application.UseCases.ContentBundles;

/// <summary>
/// Use case for retrieving a content bundle by ID
/// </summary>
public class GetContentBundleUseCase
{
    private readonly IContentBundleRepository _repository;
    private readonly ILogger<GetContentBundleUseCase> _logger;

    public GetContentBundleUseCase(
        IContentBundleRepository repository,
        ILogger<GetContentBundleUseCase> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<ContentBundle?> ExecuteAsync(string bundleId)
    {
        if (string.IsNullOrWhiteSpace(bundleId))
        {
            throw new ArgumentException("Bundle ID cannot be null or empty", nameof(bundleId));
        }

        var bundle = await _repository.GetByIdAsync(bundleId);

        if (bundle == null)
        {
            _logger.LogWarning("Content bundle not found: {BundleId}", bundleId);
        }
        else
        {
            _logger.LogDebug("Retrieved content bundle: {BundleId}", bundleId);
        }

        return bundle;
    }
}

