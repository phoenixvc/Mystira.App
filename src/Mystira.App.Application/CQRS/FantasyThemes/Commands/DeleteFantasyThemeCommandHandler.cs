using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Application.Services;

namespace Mystira.App.Application.CQRS.FantasyThemes.Commands;

/// <summary>
/// Handler for deleting a fantasy theme.
/// </summary>
public class DeleteFantasyThemeCommandHandler : ICommandHandler<DeleteFantasyThemeCommand, bool>
{
    private readonly IFantasyThemeRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IQueryCacheInvalidationService _cacheInvalidation;
    private readonly ILogger<DeleteFantasyThemeCommandHandler> _logger;

    public DeleteFantasyThemeCommandHandler(
        IFantasyThemeRepository repository,
        IUnitOfWork unitOfWork,
        IQueryCacheInvalidationService cacheInvalidation,
        ILogger<DeleteFantasyThemeCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _cacheInvalidation = cacheInvalidation;
        _logger = logger;
    }

    public async Task<bool> Handle(
        DeleteFantasyThemeCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting fantasy theme with id: {Id}", command.Id);

        var fantasyTheme = await _repository.GetByIdAsync(command.Id);
        if (fantasyTheme == null)
        {
            _logger.LogWarning("Fantasy theme with id {Id} not found", command.Id);
            return false;
        }

        await _repository.DeleteAsync(command.Id);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidate cache
        _cacheInvalidation.InvalidateCacheByPrefix("MasterData:FantasyThemes");

        _logger.LogInformation("Successfully deleted fantasy theme with id: {Id}", command.Id);
        return true;
    }
}
