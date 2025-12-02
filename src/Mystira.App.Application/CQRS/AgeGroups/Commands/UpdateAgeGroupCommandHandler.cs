using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.AgeGroups.Commands;

/// <summary>
/// Handler for updating an existing age group.
/// </summary>
public class UpdateAgeGroupCommandHandler : ICommandHandler<UpdateAgeGroupCommand, AgeGroupDefinition?>
{
    private readonly IAgeGroupRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateAgeGroupCommandHandler> _logger;

    public UpdateAgeGroupCommandHandler(
        IAgeGroupRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateAgeGroupCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<AgeGroupDefinition?> Handle(
        UpdateAgeGroupCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating age group with id: {Id}", command.Id);

        var existingAgeGroup = await _repository.GetByIdAsync(command.Id);
        if (existingAgeGroup == null)
        {
            _logger.LogWarning("Age group with id {Id} not found", command.Id);
            return null;
        }

        existingAgeGroup.Name = command.Name;
        existingAgeGroup.Value = command.Value;
        existingAgeGroup.MinimumAge = command.MinimumAge;
        existingAgeGroup.MaximumAge = command.MaximumAge;
        existingAgeGroup.Description = command.Description;
        existingAgeGroup.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(existingAgeGroup);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully updated age group with id: {Id}", command.Id);
        return existingAgeGroup;
    }
}
