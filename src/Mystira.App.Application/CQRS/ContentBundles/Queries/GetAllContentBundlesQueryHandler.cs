using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;
using Mystira.App.Domain.Specifications;

namespace Mystira.App.Application.CQRS.ContentBundles.Queries;

/// <summary>
/// Handler for GetAllContentBundlesQuery
/// Retrieves all active content bundles
/// </summary>
public class GetAllContentBundlesQueryHandler : IQueryHandler<GetAllContentBundlesQuery, IEnumerable<ContentBundle>>
{
    private readonly IContentBundleRepository _repository;
    private readonly ILogger<GetAllContentBundlesQueryHandler> _logger;

    public GetAllContentBundlesQueryHandler(
        IContentBundleRepository repository,
        ILogger<GetAllContentBundlesQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IEnumerable<ContentBundle>> Handle(
        GetAllContentBundlesQuery request,
        CancellationToken cancellationToken)
    {
        // Use specification for consistent filtering
        var spec = new ActiveContentBundlesSpecification();

        // Execute query
        var bundles = await _repository.ListAsync(spec);

        _logger.LogDebug("Retrieved {Count} content bundles", bundles.Count());

        return bundles;
    }
}
