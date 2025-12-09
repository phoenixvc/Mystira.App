using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.UseCases.BadgeConfigurations;

/// <summary>
/// Use case for retrieving badge configurations filtered by axis
/// </summary>
public class GetBadgeConfigurationsByAxisUseCase
{
    private readonly IBadgeConfigurationRepository _repository;
    private readonly ILogger<GetBadgeConfigurationsByAxisUseCase> _logger;

    public GetBadgeConfigurationsByAxisUseCase(
        IBadgeConfigurationRepository repository,
        ILogger<GetBadgeConfigurationsByAxisUseCase> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<List<BadgeConfiguration>> ExecuteAsync(string axis)
    {
        if (string.IsNullOrWhiteSpace(axis))
        {
            throw new ArgumentException("Axis cannot be null or empty", nameof(axis));
        }

        var configs = await _repository.GetByAxisAsync(axis);
        var configList = configs
            .Where(bc => bc.Axis.Equals(axis, StringComparison.OrdinalIgnoreCase))
            .OrderBy(bc => bc.Threshold)
            .ToList();

        _logger.LogInformation("Retrieved {Count} badge configurations for axis {Axis}", configList.Count, axis);
        return configList;
    }
}

