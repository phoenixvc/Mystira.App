using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.CompassAxes.Commands;

/// <summary>
/// Handler for creating a new compass axis.
/// </summary>
public class CreateCompassAxisCommandHandler : ICommandHandler<CreateCompassAxisCommand, CompassAxis>
{
    private readonly ICompassAxisRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateCompassAxisCommandHandler> _logger;

    public CreateCompassAxisCommandHandler(
        ICompassAxisRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<CreateCompassAxisCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<CompassAxis> Handle(
        CreateCompassAxisCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating compass axis: {Name}", command.Name);

        if (string.IsNullOrWhiteSpace(command.Name))
        {
            throw new ArgumentException("Name is required");
        }

        var axis = new CompassAxis
        {
            Id = Guid.NewGuid().ToString(),
            Name = command.Name,
            Description = command.Description,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(axis);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully created compass axis with id: {Id}", axis.Id);
        return axis;
    }
}
