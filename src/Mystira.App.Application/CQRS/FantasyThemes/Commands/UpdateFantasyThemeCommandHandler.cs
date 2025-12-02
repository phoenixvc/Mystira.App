using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.FantasyThemes.Commands;

/// <summary>
/// Handler for updating an existing fantasy theme.
/// </summary>
public class UpdateFantasyThemeCommandHandler : ICommandHandler<UpdateFantasyThemeCommand, FantasyThemeDefinition?>
{
    private readonly IFantasyThemeRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateFantasyThemeCommandHandler> _logger;

    public UpdateFantasyThemeCommandHandler(
        IFantasyThemeRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateFantasyThemeCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<FantasyThemeDefinition?> Handle(
        UpdateFantasyThemeCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating fantasy theme with id: {Id}", command.Id);

        var existingFantasyTheme = await _repository.GetByIdAsync(command.Id);
        if (existingFantasyTheme == null)
        {
            _logger.LogWarning("Fantasy theme with id {Id} not found", command.Id);
            return null;
        }

        existingFantasyTheme.Name = command.Name;
        existingFantasyTheme.Description = command.Description;
        existingFantasyTheme.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(existingFantasyTheme);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully updated fantasy theme with id: {Id}", command.Id);
        return existingFantasyTheme;
    }
}
