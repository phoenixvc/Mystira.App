using Microsoft.Extensions.Logging;
using Mystira.App.Domain.Models;
using Mystira.App.Infrastructure.Data.Repositories;

namespace Mystira.App.Application.UseCases.ContentBundles;

/// <summary>
/// Use case for retrieving content bundles filtered by age group
/// </summary>
public class GetContentBundlesByAgeGroupUseCase
{
    private readonly IContentBundleRepository _repository;
    private readonly ILogger<GetContentBundlesByAgeGroupUseCase> _logger;

    public GetContentBundlesByAgeGroupUseCase(
        IContentBundleRepository repository,
        ILogger<GetContentBundlesByAgeGroupUseCase> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<List<ContentBundle>> ExecuteAsync(string ageGroup)
    {
        if (string.IsNullOrWhiteSpace(ageGroup))
        {
            throw new ArgumentException("Age group cannot be null or empty", nameof(ageGroup));
        }

        var bundles = await _repository.GetByAgeGroupAsync(ageGroup);
        var bundleList = bundles.ToList();

        _logger.LogInformation("Retrieved {Count} content bundles for age group {AgeGroup}", bundleList.Count, ageGroup);
        return bundleList;
    }
}

