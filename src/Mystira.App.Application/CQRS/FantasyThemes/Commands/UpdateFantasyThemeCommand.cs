using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.FantasyThemes.Commands;

/// <summary>
/// Command to update an existing fantasy theme.
/// </summary>
public record UpdateFantasyThemeCommand(string Id, string Name, string Description) : ICommand<FantasyThemeDefinition?>;
