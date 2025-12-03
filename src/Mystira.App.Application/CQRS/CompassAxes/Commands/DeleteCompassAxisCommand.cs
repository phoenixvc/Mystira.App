namespace Mystira.App.Application.CQRS.CompassAxes.Commands;

/// <summary>
/// Command to delete a compass axis.
/// </summary>
public record DeleteCompassAxisCommand(string Id) : ICommand<bool>;
