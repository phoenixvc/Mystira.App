using Microsoft.Extensions.Logging;
using Mystira.App.Application.Interfaces;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.BadgeConfigurations.Queries;

public class GetBadgeConfigurationQueryHandler : IQueryHandler<GetBadgeConfigurationQuery, BadgeConfiguration?>
{
    private readonly IBadgeConfigurationRepository _repository;
    private readonly ILogger<GetBadgeConfigurationQueryHandler> _logger;

    public GetBadgeConfigurationQueryHandler(
        IBadgeConfigurationRepository repository,
        ILogger<GetBadgeConfigurationQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<BadgeConfiguration?> Handle(
        GetBadgeConfigurationQuery request,
        CancellationToken cancellationToken)
    {
        var badge = await _repository.GetByIdAsync(request.BadgeId);
        _logger.LogDebug("Retrieved badge configuration {BadgeId}", request.BadgeId);
        return badge;
    }
}
