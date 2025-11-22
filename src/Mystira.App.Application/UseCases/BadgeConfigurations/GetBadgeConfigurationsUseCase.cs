using Microsoft.Extensions.Logging;
using Mystira.App.Domain.Models;
using Mystira.App.Infrastructure.Data.Repositories;

namespace Mystira.App.Application.UseCases.BadgeConfigurations;

/// <summary>
/// Use case for retrieving all badge configurations
/// </summary>
public class GetBadgeConfigurationsUseCase
{
    private readonly IBadgeConfigurationRepository _repository;
    private readonly ILogger<GetBadgeConfigurationsUseCase> _logger;

    public GetBadgeConfigurationsUseCase(
        IBadgeConfigurationRepository repository,
        ILogger<GetBadgeConfigurationsUseCase> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<List<BadgeConfiguration>> ExecuteAsync()
    {
        var badgeConfigs = await _repository.GetAllAsync();
        var badgeConfigList = badgeConfigs.ToList();

        _logger.LogInformation("Retrieved {Count} badge configurations", badgeConfigList.Count);
        return badgeConfigList;
    }
}

