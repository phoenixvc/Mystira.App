using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Application.Services;

namespace Mystira.App.Application.CQRS.EchoTypes.Commands;

/// <summary>
/// Handler for deleting an echo type.
/// </summary>
public class DeleteEchoTypeCommandHandler : ICommandHandler<DeleteEchoTypeCommand, bool>
{
    private readonly IEchoTypeRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IQueryCacheInvalidationService _cacheInvalidation;
    private readonly ILogger<DeleteEchoTypeCommandHandler> _logger;

    public DeleteEchoTypeCommandHandler(
        IEchoTypeRepository repository,
        IUnitOfWork unitOfWork,
        IQueryCacheInvalidationService cacheInvalidation,
        ILogger<DeleteEchoTypeCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _cacheInvalidation = cacheInvalidation;
        _logger = logger;
    }

    public async Task<bool> Handle(
        DeleteEchoTypeCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting echo type with id: {Id}", command.Id);

        var echoType = await _repository.GetByIdAsync(command.Id);
        if (echoType == null)
        {
            _logger.LogWarning("Echo type with id {Id} not found", command.Id);
            return false;
        }

        await _repository.DeleteAsync(command.Id);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidate cache
        _cacheInvalidation.InvalidateCacheByPrefix("MasterData:EchoTypes");

        _logger.LogInformation("Successfully deleted echo type with id: {Id}", command.Id);
        return true;
    }
}
