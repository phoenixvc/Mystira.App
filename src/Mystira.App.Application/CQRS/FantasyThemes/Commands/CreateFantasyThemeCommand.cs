using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.FantasyThemes.Commands;

/// <summary>
/// Command to create a new fantasy theme.
/// </summary>
public record CreateFantasyThemeCommand(string Name, string Description) : ICommand<FantasyThemeDefinition>;
