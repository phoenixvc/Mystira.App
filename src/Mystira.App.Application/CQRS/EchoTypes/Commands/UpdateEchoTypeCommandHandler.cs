using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Application.Services;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.EchoTypes.Commands;

/// <summary>
/// Handler for updating an existing echo type.
/// </summary>
public class UpdateEchoTypeCommandHandler : ICommandHandler<UpdateEchoTypeCommand, EchoTypeDefinition?>
{
    private readonly IEchoTypeRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IQueryCacheInvalidationService _cacheInvalidation;
    private readonly ILogger<UpdateEchoTypeCommandHandler> _logger;

    public UpdateEchoTypeCommandHandler(
        IEchoTypeRepository repository,
        IUnitOfWork unitOfWork,
        IQueryCacheInvalidationService cacheInvalidation,
        ILogger<UpdateEchoTypeCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _cacheInvalidation = cacheInvalidation;
        _logger = logger;
    }

    public async Task<EchoTypeDefinition?> Handle(
        UpdateEchoTypeCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating echo type with id: {Id}", command.Id);

        var existingEchoType = await _repository.GetByIdAsync(command.Id);
        if (existingEchoType == null)
        {
            _logger.LogWarning("Echo type with id {Id} not found", command.Id);
            return null;
        }

        existingEchoType.Name = command.Name;
        existingEchoType.Description = command.Description;
        existingEchoType.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(existingEchoType);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidate cache
        _cacheInvalidation.InvalidateCacheByPrefix("MasterData:EchoTypes");

        _logger.LogInformation("Successfully updated echo type with id: {Id}", command.Id);
        return existingEchoType;
    }
}
