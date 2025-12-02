using Microsoft.Extensions.Logging;
using Mystira.App.Contracts.Requests.Badges;
using Mystira.App.Domain.Models;
using Mystira.App.Application.Ports.Data;

namespace Mystira.App.Application.UseCases.BadgeConfigurations;

/// <summary>
/// Use case for updating a badge configuration
/// </summary>
public class UpdateBadgeConfigurationUseCase
{
    private readonly IBadgeConfigurationRepository _repository;
    private readonly ICompassAxisRepository _compassAxisRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateBadgeConfigurationUseCase> _logger;

    public UpdateBadgeConfigurationUseCase(
        IBadgeConfigurationRepository repository,
        ICompassAxisRepository compassAxisRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateBadgeConfigurationUseCase> logger)
    {
        _repository = repository;
        _compassAxisRepository = compassAxisRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<BadgeConfiguration> ExecuteAsync(string badgeConfigurationId, UpdateBadgeConfigurationRequest request)
    {
        if (string.IsNullOrWhiteSpace(badgeConfigurationId))
        {
            throw new ArgumentException("Badge configuration ID cannot be null or empty", nameof(badgeConfigurationId));
        }

        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var badgeConfig = await _repository.GetByIdAsync(badgeConfigurationId);
        if (badgeConfig == null)
        {
            throw new ArgumentException($"Badge configuration not found: {badgeConfigurationId}", nameof(badgeConfigurationId));
        }

        // Update properties if provided
        if (request.Name != null)
        {
            badgeConfig.Name = request.Name;
        }

        if (request.Message != null)
        {
            badgeConfig.Message = request.Message;
        }

        if (request.Axis != null)
        {
            // Validate that the axis exists in the database
            var axisExists = await _compassAxisRepository.ExistsByNameAsync(request.Axis);
            if (!axisExists)
            {
                var allAxes = await _compassAxisRepository.GetAllAsync();
                throw new ArgumentException($"Invalid compass axis: {request.Axis}. Must be one of: {string.Join(", ", allAxes.Select(a => a.Name))}", nameof(request));
            }
            badgeConfig.Axis = request.Axis;
        }

        if (request.Threshold.HasValue)
        {
            badgeConfig.Threshold = request.Threshold.Value;
        }

        if (request.ImageId != null)
        {
            badgeConfig.ImageId = request.ImageId;
        }

        badgeConfig.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(badgeConfig);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Updated badge configuration: {BadgeConfigurationId}", badgeConfigurationId);
        return badgeConfig;
    }
}

