using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;

namespace Mystira.App.Application.CQRS.CompassAxes.Commands;

/// <summary>
/// Handler for deleting a compass axis.
/// </summary>
public class DeleteCompassAxisCommandHandler : ICommandHandler<DeleteCompassAxisCommand, bool>
{
    private readonly ICompassAxisRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteCompassAxisCommandHandler> _logger;

    public DeleteCompassAxisCommandHandler(
        ICompassAxisRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCompassAxisCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> Handle(
        DeleteCompassAxisCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting compass axis with id: {Id}", command.Id);

        var axis = await _repository.GetByIdAsync(command.Id);
        if (axis == null)
        {
            _logger.LogWarning("Compass axis with id {Id} not found", command.Id);
            return false;
        }

        await _repository.DeleteAsync(command.Id);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully deleted compass axis with id: {Id}", command.Id);
        return true;
    }
}
