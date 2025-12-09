using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.FantasyThemes.Queries;

/// <summary>
/// Handler for retrieving all fantasy themes.
/// </summary>
public class GetAllFantasyThemesQueryHandler : IQueryHandler<GetAllFantasyThemesQuery, List<FantasyThemeDefinition>>
{
    private readonly IFantasyThemeRepository _repository;
    private readonly ILogger<GetAllFantasyThemesQueryHandler> _logger;

    public GetAllFantasyThemesQueryHandler(
        IFantasyThemeRepository repository,
        ILogger<GetAllFantasyThemesQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<List<FantasyThemeDefinition>> Handle(
        GetAllFantasyThemesQuery query,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving all fantasy themes");
        var fantasyThemes = await _repository.GetAllAsync();
        _logger.LogInformation("Retrieved {Count} fantasy themes", fantasyThemes.Count);
        return fantasyThemes;
    }
}
