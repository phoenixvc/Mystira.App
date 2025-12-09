using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.UseCases.ContentBundles;

/// <summary>
/// Use case for retrieving all content bundles
/// </summary>
public class GetContentBundlesUseCase
{
    private readonly IContentBundleRepository _repository;
    private readonly ILogger<GetContentBundlesUseCase> _logger;

    public GetContentBundlesUseCase(
        IContentBundleRepository repository,
        ILogger<GetContentBundlesUseCase> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<List<ContentBundle>> ExecuteAsync()
    {
        var bundles = await _repository.GetAllAsync();
        var bundleList = bundles.ToList();

        _logger.LogInformation("Retrieved {Count} content bundles", bundleList.Count);
        return bundleList;
    }
}

