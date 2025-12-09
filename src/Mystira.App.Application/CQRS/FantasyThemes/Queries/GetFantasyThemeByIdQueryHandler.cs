using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.FantasyThemes.Queries;

/// <summary>
/// Handler for retrieving a fantasy theme by ID.
/// </summary>
public class GetFantasyThemeByIdQueryHandler : IQueryHandler<GetFantasyThemeByIdQuery, FantasyThemeDefinition?>
{
    private readonly IFantasyThemeRepository _repository;
    private readonly ILogger<GetFantasyThemeByIdQueryHandler> _logger;

    public GetFantasyThemeByIdQueryHandler(
        IFantasyThemeRepository repository,
        ILogger<GetFantasyThemeByIdQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<FantasyThemeDefinition?> Handle(
        GetFantasyThemeByIdQuery query,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving fantasy theme with id: {Id}", query.Id);
        var fantasyTheme = await _repository.GetByIdAsync(query.Id);

        if (fantasyTheme == null)
        {
            _logger.LogWarning("Fantasy theme with id {Id} not found", query.Id);
        }

        return fantasyTheme;
    }
}
