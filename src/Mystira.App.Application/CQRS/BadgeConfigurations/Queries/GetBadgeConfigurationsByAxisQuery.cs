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
{
    private readonly IBadgeConfigurationRepository _repository;

    public GetBadgeConfigurationsByAxisQueryHandler(IBadgeConfigurationRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<BadgeConfiguration>> Handle(GetBadgeConfigurationsByAxisQuery request, CancellationToken cancellationToken)
    {
        var list = await _repository.GetByAxisAsync(request.Axis);
        return list.OrderBy(b => b.Name).ToList();
    }
}
