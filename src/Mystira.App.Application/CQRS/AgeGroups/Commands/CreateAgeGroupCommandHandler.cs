using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.AgeGroups.Commands;

/// <summary>
/// Handler for creating a new age group.
/// </summary>
public class CreateAgeGroupCommandHandler : ICommandHandler<CreateAgeGroupCommand, AgeGroupDefinition>
{
    private readonly IAgeGroupRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateAgeGroupCommandHandler> _logger;

    public CreateAgeGroupCommandHandler(
        IAgeGroupRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<CreateAgeGroupCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<AgeGroupDefinition> Handle(
        CreateAgeGroupCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating age group: {Name}", command.Name);

        if (string.IsNullOrWhiteSpace(command.Name))
        {
            throw new ArgumentException("Name is required");
        }

        if (string.IsNullOrWhiteSpace(command.Value))
        {
            throw new ArgumentException("Value is required");
        }

        var ageGroup = new AgeGroupDefinition
        {
            Id = Guid.NewGuid().ToString(),
            Name = command.Name,
            Value = command.Value,
            MinimumAge = command.MinimumAge,
            MaximumAge = command.MaximumAge,
            Description = command.Description,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(ageGroup);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully created age group with id: {Id}", ageGroup.Id);
        return ageGroup;
    }
}
