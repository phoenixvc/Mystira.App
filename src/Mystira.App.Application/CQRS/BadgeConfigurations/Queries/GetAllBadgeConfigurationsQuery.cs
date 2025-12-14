using Mystira.App.Application.CQRS;
using Mystira.App.Application.Interfaces;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.BadgeConfigurations.Queries;

public record GetAllBadgeConfigurationsQuery
    : IQuery<List<BadgeConfiguration>>, ICacheableQuery
{
    public string CacheKey => "BadgeConfigurations:All";
}

public sealed class GetAllBadgeConfigurationsQueryHandler
    : IQueryHandler<GetAllBadgeConfigurationsQuery, List<BadgeConfiguration>>
{
    private readonly IRepository<BadgeConfiguration> _repository;

    public GetAllBadgeConfigurationsQueryHandler(IRepository<BadgeConfiguration> repository)
    {
        _repository = repository;
    }

    public async Task<List<BadgeConfiguration>> Handle(GetAllBadgeConfigurationsQuery request, CancellationToken cancellationToken)
    {
        var all = await _repository.GetAllAsync();
        // Stable ordering by Name for deterministic results in tests/clients
        return all.OrderBy(b => b.Name).ToList();
    }
}
