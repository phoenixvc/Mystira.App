using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.EchoTypes.Commands;

/// <summary>
/// Handler for creating a new echo type.
/// </summary>
public class CreateEchoTypeCommandHandler : ICommandHandler<CreateEchoTypeCommand, EchoTypeDefinition>
{
    private readonly IEchoTypeRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateEchoTypeCommandHandler> _logger;

    public CreateEchoTypeCommandHandler(
        IEchoTypeRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<CreateEchoTypeCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<EchoTypeDefinition> Handle(
        CreateEchoTypeCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating echo type: {Name}", command.Name);

        if (string.IsNullOrWhiteSpace(command.Name))
        {
            throw new ArgumentException("Name is required");
        }

        var echoType = new EchoTypeDefinition
        {
            Id = Guid.NewGuid().ToString(),
            Name = command.Name,
            Description = command.Description,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(echoType);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully created echo type with id: {Id}", echoType.Id);
        return echoType;
    }
}
