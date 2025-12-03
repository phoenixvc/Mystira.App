using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Application.Services;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.FantasyThemes.Commands;

/// <summary>
/// Handler for creating a new fantasy theme.
/// </summary>
public class CreateFantasyThemeCommandHandler : ICommandHandler<CreateFantasyThemeCommand, FantasyThemeDefinition>
{
    private readonly IFantasyThemeRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IQueryCacheInvalidationService _cacheInvalidation;
    private readonly ILogger<CreateFantasyThemeCommandHandler> _logger;

    public CreateFantasyThemeCommandHandler(
        IFantasyThemeRepository repository,
        IUnitOfWork unitOfWork,
        IQueryCacheInvalidationService cacheInvalidation,
        ILogger<CreateFantasyThemeCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _cacheInvalidation = cacheInvalidation;
        _logger = logger;
    }

    public async Task<FantasyThemeDefinition> Handle(
        CreateFantasyThemeCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating fantasy theme: {Name}", command.Name);

        if (string.IsNullOrWhiteSpace(command.Name))
        {
            throw new ArgumentException("Name is required");
        }

        var fantasyTheme = new FantasyThemeDefinition
        {
            Id = Guid.NewGuid().ToString(),
            Name = command.Name,
            Description = command.Description,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(fantasyTheme);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidate cache
        _cacheInvalidation.InvalidateCacheByPrefix("MasterData:FantasyThemes");

        _logger.LogInformation("Successfully created fantasy theme with id: {Id}", fantasyTheme.Id);
        return fantasyTheme;
    }
}
