using Mystira.App.Application.Interfaces;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.EchoTypes.Queries;

/// <summary>
/// Query to retrieve an echo type by ID.
/// </summary>
public record GetEchoTypeByIdQuery(string Id) : IQuery<EchoTypeDefinition?>, ICacheableQuery
{
    public string CacheKey => $"MasterData:EchoTypes:{Id}";
    public int CacheDurationSeconds => 3600; // 1 hour - master data rarely changes
}
