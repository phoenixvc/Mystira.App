using Microsoft.Extensions.Logging;
using Mystira.App.Domain.Models;
using Mystira.App.Application.Ports.Data;

namespace Mystira.App.Application.UseCases.BadgeConfigurations;

/// <summary>
/// Use case for retrieving a badge configuration by ID
/// </summary>
public class GetBadgeConfigurationUseCase
{
    private readonly IBadgeConfigurationRepository _repository;
    private readonly ILogger<GetBadgeConfigurationUseCase> _logger;

    public GetBadgeConfigurationUseCase(
        IBadgeConfigurationRepository repository,
        ILogger<GetBadgeConfigurationUseCase> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<BadgeConfiguration?> ExecuteAsync(string badgeConfigurationId)
    {
        if (string.IsNullOrWhiteSpace(badgeConfigurationId))
        {
            throw new ArgumentException("Badge configuration ID cannot be null or empty", nameof(badgeConfigurationId));
        }

        var badgeConfig = await _repository.GetByIdAsync(badgeConfigurationId);

        if (badgeConfig == null)
        {
            _logger.LogWarning("Badge configuration not found: {BadgeConfigurationId}", badgeConfigurationId);
        }
        else
        {
            _logger.LogDebug("Retrieved badge configuration: {BadgeConfigurationId}", badgeConfigurationId);
        }

        return badgeConfig;
    }
}

