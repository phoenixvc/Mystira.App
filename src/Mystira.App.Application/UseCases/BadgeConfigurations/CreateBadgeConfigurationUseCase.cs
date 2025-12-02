using Microsoft.Extensions.Logging;
using Mystira.App.Contracts.Requests.Badges;
using Mystira.App.Domain.Models;
using Mystira.App.Application.Ports.Data;

namespace Mystira.App.Application.UseCases.BadgeConfigurations;

/// <summary>
/// Use case for creating a new badge configuration
/// </summary>
public class CreateBadgeConfigurationUseCase
{
    private readonly IBadgeConfigurationRepository _repository;
    private readonly ICompassAxisRepository _compassAxisRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateBadgeConfigurationUseCase> _logger;

    public CreateBadgeConfigurationUseCase(
        IBadgeConfigurationRepository repository,
        ICompassAxisRepository compassAxisRepository,
        IUnitOfWork unitOfWork,
        ILogger<CreateBadgeConfigurationUseCase> logger)
    {
        _repository = repository;
        _compassAxisRepository = compassAxisRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<BadgeConfiguration> ExecuteAsync(CreateBadgeConfigurationRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        // Check if badge configuration with ID already exists
        var existingBadgeConfig = await _repository.GetByIdAsync(request.Id);
        if (existingBadgeConfig != null)
        {
            throw new InvalidOperationException($"Badge configuration with ID {request.Id} already exists");
        }

        // Validate that the axis exists in the database
        var axisExists = await _compassAxisRepository.ExistsByNameAsync(request.Axis);
        if (!axisExists)
        {
            var allAxes = await _compassAxisRepository.GetAllAsync();
            throw new ArgumentException($"Invalid compass axis: {request.Axis}. Must be one of: {string.Join(", ", allAxes.Select(a => a.Name))}", nameof(request));
        }

        var badgeConfig = new BadgeConfiguration
        {
            Id = request.Id,
            Name = request.Name,
            Message = request.Message,
            Axis = request.Axis,
            Threshold = request.Threshold,
            ImageId = request.ImageId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(badgeConfig);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Created badge configuration: {BadgeConfigId} - {Name}", badgeConfig.Id, badgeConfig.Name);
        return badgeConfig;
    }
}

