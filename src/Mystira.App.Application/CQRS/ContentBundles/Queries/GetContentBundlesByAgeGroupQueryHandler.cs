using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Application.Specifications;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.ContentBundles.Queries;

/// <summary>
/// Handler for GetContentBundlesByAgeGroupQuery
/// Retrieves content bundles filtered by age group
/// </summary>
public class GetContentBundlesByAgeGroupQueryHandler : IQueryHandler<GetContentBundlesByAgeGroupQuery, IEnumerable<ContentBundle>>
{
    private readonly IContentBundleRepository _repository;
    private readonly ILogger<GetContentBundlesByAgeGroupQueryHandler> _logger;

    public GetContentBundlesByAgeGroupQueryHandler(
        IContentBundleRepository repository,
        ILogger<GetContentBundlesByAgeGroupQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IEnumerable<ContentBundle>> Handle(
        GetContentBundlesByAgeGroupQuery request,
        CancellationToken cancellationToken)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(request.AgeGroup))
        {
            _logger.LogWarning("Age group cannot be null or empty");
            throw new ArgumentException("Age group cannot be null or empty", nameof(request.AgeGroup));
        }

        // Create specification for reusable query logic
        var spec = new ContentBundlesByAgeGroupSpec(request.AgeGroup);

        // Execute query using repository
        var bundles = await _repository.ListAsync(spec);

        _logger.LogDebug("Retrieved {Count} bundles for age group {AgeGroup}",
            bundles.Count(), request.AgeGroup);

        return bundles;
    }
}
