using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.EchoTypes.Queries;

/// <summary>
/// Wolverine handler for retrieving all echo types.
/// </summary>
public static class GetAllEchoTypesQueryHandler
{
    public static async Task<List<EchoTypeDefinition>> Handle(
        GetAllEchoTypesQuery query,
        IEchoTypeRepository repository,
        ILogger<GetAllEchoTypesQuery> logger,
        CancellationToken ct)
    {
        logger.LogInformation("Retrieving all echo types");
        var echoTypes = await repository.GetAllAsync();
        logger.LogInformation("Retrieved {Count} echo types", echoTypes.Count);
        return echoTypes;
    }
}
