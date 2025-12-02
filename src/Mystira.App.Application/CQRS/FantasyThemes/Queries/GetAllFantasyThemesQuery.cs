using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.FantasyThemes.Queries;

/// <summary>
/// Query to retrieve all fantasy themes.
/// </summary>
public record GetAllFantasyThemesQuery : IQuery<List<FantasyThemeDefinition>>;
