using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.EchoTypes.Queries;

/// <summary>
/// Wolverine handler for retrieving an echo type by ID.
/// </summary>
public static class GetEchoTypeByIdQueryHandler
{
    public static async Task<EchoTypeDefinition?> Handle(
        GetEchoTypeByIdQuery query,
        IEchoTypeRepository repository,
        ILogger<GetEchoTypeByIdQuery> logger,
        CancellationToken ct)
    {
        logger.LogInformation("Retrieving echo type with id: {Id}", query.Id);
        var echoType = await repository.GetByIdAsync(query.Id);

        if (echoType == null)
        {
            logger.LogWarning("Echo type with id {Id} not found", query.Id);
        }

        return echoType;
    }
}
