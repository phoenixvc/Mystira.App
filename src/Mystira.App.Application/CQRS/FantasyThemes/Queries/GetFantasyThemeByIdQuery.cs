using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.FantasyThemes.Queries;

/// <summary>
/// Query to retrieve a fantasy theme by ID.
/// </summary>
public record GetFantasyThemeByIdQuery(string Id) : IQuery<FantasyThemeDefinition?>;
