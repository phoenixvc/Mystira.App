using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.AgeGroups.Queries;

/// <summary>
/// Wolverine handler for retrieving all age groups.
/// </summary>
public static class GetAllAgeGroupsQueryHandler
{
    public static async Task<List<AgeGroupDefinition>> Handle(
        GetAllAgeGroupsQuery query,
        IAgeGroupRepository repository,
        ILogger logger,
        CancellationToken ct)
    {
        logger.LogInformation("Retrieving all age groups");
        var ageGroups = await repository.GetAllAsync();
        logger.LogInformation("Retrieved {Count} age groups", ageGroups.Count);
        return ageGroups;
    }
}
