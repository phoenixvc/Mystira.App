using Mystira.App.Application.Interfaces;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.EchoTypes.Queries;

/// <summary>
/// Query to retrieve all echo types.
/// </summary>
public record GetAllEchoTypesQuery : IQuery<List<EchoTypeDefinition>>, ICacheableQuery
{
    public string CacheKey => "MasterData:EchoTypes:All";
    public int CacheDurationSeconds => 3600; // 1 hour - master data rarely changes
}
