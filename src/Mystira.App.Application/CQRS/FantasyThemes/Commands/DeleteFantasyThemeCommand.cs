namespace Mystira.App.Application.CQRS.FantasyThemes.Commands;

/// <summary>
/// Command to delete a fantasy theme.
/// </summary>
public record DeleteFantasyThemeCommand(string Id) : ICommand<bool>;
