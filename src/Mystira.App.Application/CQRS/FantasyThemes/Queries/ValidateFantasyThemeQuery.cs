namespace Mystira.App.Application.CQRS.FantasyThemes.Queries;

/// <summary>
/// Query to validate if a fantasy theme name exists.
/// </summary>
public record ValidateFantasyThemeQuery(string Name) : IQuery<bool>;
