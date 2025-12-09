using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.AgeGroups.Queries;

/// <summary>
/// Handler for retrieving all age groups.
/// </summary>
public class GetAllAgeGroupsQueryHandler : IQueryHandler<GetAllAgeGroupsQuery, List<AgeGroupDefinition>>
{
    private readonly IAgeGroupRepository _repository;
    private readonly ILogger<GetAllAgeGroupsQueryHandler> _logger;

    public GetAllAgeGroupsQueryHandler(
        IAgeGroupRepository repository,
        ILogger<GetAllAgeGroupsQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<List<AgeGroupDefinition>> Handle(
        GetAllAgeGroupsQuery query,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving all age groups");
        var ageGroups = await _repository.GetAllAsync();
        _logger.LogInformation("Retrieved {Count} age groups", ageGroups.Count);
        return ageGroups;
    }
}
