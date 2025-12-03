using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Application.Services;

namespace Mystira.App.Application.CQRS.AgeGroups.Commands;

/// <summary>
/// Handler for deleting an age group.
/// </summary>
public class DeleteAgeGroupCommandHandler : ICommandHandler<DeleteAgeGroupCommand, bool>
{
    private readonly IAgeGroupRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IQueryCacheInvalidationService _cacheInvalidation;
    private readonly ILogger<DeleteAgeGroupCommandHandler> _logger;

    public DeleteAgeGroupCommandHandler(
        IAgeGroupRepository repository,
        IUnitOfWork unitOfWork,
        IQueryCacheInvalidationService cacheInvalidation,
        ILogger<DeleteAgeGroupCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _cacheInvalidation = cacheInvalidation;
        _logger = logger;
    }

    public async Task<bool> Handle(
        DeleteAgeGroupCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting age group with id: {Id}", command.Id);

        var ageGroup = await _repository.GetByIdAsync(command.Id);
        if (ageGroup == null)
        {
            _logger.LogWarning("Age group with id {Id} not found", command.Id);
            return false;
        }

        await _repository.DeleteAsync(command.Id);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidate cache
        _cacheInvalidation.InvalidateCacheByPrefix("MasterData:AgeGroups");

        _logger.LogInformation("Successfully deleted age group with id: {Id}", command.Id);
        return true;
    }
}
