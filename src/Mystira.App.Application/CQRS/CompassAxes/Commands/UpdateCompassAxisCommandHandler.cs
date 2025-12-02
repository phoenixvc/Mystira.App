using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Application.Services;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.CompassAxes.Commands;

/// <summary>
/// Handler for updating an existing compass axis.
/// </summary>
public class UpdateCompassAxisCommandHandler : ICommandHandler<UpdateCompassAxisCommand, CompassAxis?>
{
    private readonly ICompassAxisRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IQueryCacheInvalidationService _cacheInvalidation;
    private readonly ILogger<UpdateCompassAxisCommandHandler> _logger;

    public UpdateCompassAxisCommandHandler(
        ICompassAxisRepository repository,
        IUnitOfWork unitOfWork,
        IQueryCacheInvalidationService cacheInvalidation,
        ILogger<UpdateCompassAxisCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _cacheInvalidation = cacheInvalidation;
        _logger = logger;
    }

    public async Task<CompassAxis?> Handle(
        UpdateCompassAxisCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating compass axis with id: {Id}", command.Id);

        var existingAxis = await _repository.GetByIdAsync(command.Id);
        if (existingAxis == null)
        {
            _logger.LogWarning("Compass axis with id {Id} not found", command.Id);
            return null;
        }

        existingAxis.Name = command.Name;
        existingAxis.Description = command.Description;
        existingAxis.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(existingAxis);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidate cache
        _cacheInvalidation.InvalidateCacheByPrefix("MasterData:CompassAxes");

        _logger.LogInformation("Successfully updated compass axis with id: {Id}", command.Id);
        return existingAxis;
    }
}
