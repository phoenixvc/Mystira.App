using Mystira.Shared.CQRS;
using Mystira.App.Application.Interfaces;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.BadgeConfigurations.Queries;

public record GetBadgeConfigurationsByAxisQuery(string Axis)
    : IQuery<List<BadgeConfiguration>>, ICacheableQuery
{
    public string CacheKey => $"BadgeConfigurations:Axis:{Axis}";
}

public sealed class GetBadgeConfigurationsByAxisQueryHandler
    : IQueryHandler<GetBadgeConfigurationsByAxisQuery, List<BadgeConfiguration>>
{
    private readonly IRepository<BadgeConfiguration> _repository;

    public GetBadgeConfigurationsByAxisQueryHandler(IRepository<BadgeConfiguration> repository)
    {
        _repository = repository;
    }

    public async Task<List<BadgeConfiguration>> Handle(GetBadgeConfigurationsByAxisQuery request, CancellationToken cancellationToken)
    {
        var list = await _repository.FindAsync(b => b.Axis == request.Axis);
        return list.OrderBy(b => b.Name).ToList();
    }
}
