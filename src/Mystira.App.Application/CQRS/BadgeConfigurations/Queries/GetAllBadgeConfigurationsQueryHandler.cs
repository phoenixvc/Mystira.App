using Microsoft.Extensions.Logging;
using Mystira.App.Application.Interfaces;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.BadgeConfigurations.Queries;

public class GetAllBadgeConfigurationsQueryHandler : IQueryHandler<GetAllBadgeConfigurationsQuery, List<BadgeConfiguration>>
{
    private readonly IBadgeConfigurationRepository _repository;
    private readonly ILogger<GetAllBadgeConfigurationsQueryHandler> _logger;

    public GetAllBadgeConfigurationsQueryHandler(
        IBadgeConfigurationRepository repository,
        ILogger<GetAllBadgeConfigurationsQueryHandler> _logger)
    {
        _repository = repository;
        this._logger = _logger;
    }

    public async Task<List<BadgeConfiguration>> Handle(
        GetAllBadgeConfigurationsQuery request,
        CancellationToken cancellationToken)
    {
        var badges = await _repository.ListAllAsync();
        _logger.LogDebug("Retrieved {Count} badge configurations", badges.Count());
        return badges.ToList();
    }
}
