using Mystira.Shared.CQRS;
using Mystira.App.Application.Interfaces;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.BadgeConfigurations.Queries;

public record GetBadgeConfigurationQuery(string Id)
    : IQuery<BadgeConfiguration?>, ICacheableQuery
{
    public string CacheKey => $"BadgeConfigurations:Id:{Id}";
}

public sealed class GetBadgeConfigurationQueryHandler
{
    private readonly IRepository<BadgeConfiguration, string> _repository;

    public GetBadgeConfigurationQueryHandler(IRepository<BadgeConfiguration, string> repository)
    {
        _repository = repository;
    }

    public async Task<BadgeConfiguration?> Handle(GetBadgeConfigurationQuery request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(request.Id);
        if (entity == null) return null;
        return Clone(entity);
    }

    private static BadgeConfiguration Clone(BadgeConfiguration s) => new()
    {
        Id = s.Id,
        Name = s.Name,
        Message = s.Message,
        Axis = s.Axis,
        Threshold = s.Threshold,
        ImageId = s.ImageId,
        CreatedAt = s.CreatedAt,
        UpdatedAt = s.UpdatedAt
    };
}
