using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.Archetypes.Commands;

/// <summary>
/// Handler for creating a new archetype.
/// </summary>
public class CreateArchetypeCommandHandler : ICommandHandler<CreateArchetypeCommand, ArchetypeDefinition>
{
    private readonly IArchetypeRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateArchetypeCommandHandler> _logger;

    public CreateArchetypeCommandHandler(
        IArchetypeRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<CreateArchetypeCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ArchetypeDefinition> Handle(
        CreateArchetypeCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating archetype: {Name}", command.Name);

        if (string.IsNullOrWhiteSpace(command.Name))
        {
            throw new ArgumentException("Name is required");
        }

        var archetype = new ArchetypeDefinition
        {
            Id = Guid.NewGuid().ToString(),
            Name = command.Name,
            Description = command.Description,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(archetype);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully created archetype with id: {Id}", archetype.Id);
        return archetype;
    }
}
