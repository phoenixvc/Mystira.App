using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;
using Mystira.App.Domain.Specifications;

namespace Mystira.App.Application.CQRS.BadgeConfigurations.Queries;

public class GetBadgeConfigurationsByAxisQueryHandler : IQueryHandler<GetBadgeConfigurationsByAxisQuery, List<BadgeConfiguration>>
{
    private readonly IBadgeConfigurationRepository _repository;
    private readonly ILogger<GetBadgeConfigurationsByAxisQueryHandler> _logger;

    public GetBadgeConfigurationsByAxisQueryHandler(
        IBadgeConfigurationRepository repository,
        ILogger<GetBadgeConfigurationsByAxisQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<List<BadgeConfiguration>> Handle(
        GetBadgeConfigurationsByAxisQuery request,
        CancellationToken cancellationToken)
    {
        var spec = new BadgeConfigurationsByAxisSpecification(request.Axis);
        var badges = await _repository.ListAsync(spec);
        _logger.LogDebug("Retrieved {Count} badge configurations for axis {Axis}",
            badges.Count(), request.Axis);
        return badges.ToList();
    }
}
