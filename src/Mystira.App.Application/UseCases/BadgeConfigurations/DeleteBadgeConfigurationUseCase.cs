using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;

namespace Mystira.App.Application.UseCases.BadgeConfigurations;

/// <summary>
/// Use case for deleting a badge configuration
/// </summary>
public class DeleteBadgeConfigurationUseCase
{
    private readonly IBadgeConfigurationRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteBadgeConfigurationUseCase> _logger;

    public DeleteBadgeConfigurationUseCase(
        IBadgeConfigurationRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<DeleteBadgeConfigurationUseCase> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> ExecuteAsync(string badgeConfigurationId)
    {
        if (string.IsNullOrWhiteSpace(badgeConfigurationId))
        {
            throw new ArgumentException("Badge configuration ID cannot be null or empty", nameof(badgeConfigurationId));
        }

        var badgeConfig = await _repository.GetByIdAsync(badgeConfigurationId);
        if (badgeConfig == null)
        {
            _logger.LogWarning("Badge configuration not found for deletion: {BadgeConfigurationId}", badgeConfigurationId);
            return false;
        }

        await _repository.DeleteAsync(badgeConfigurationId);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Deleted badge configuration: {BadgeConfigurationId}", badgeConfigurationId);
        return true;
    }
}

