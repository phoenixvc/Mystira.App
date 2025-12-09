using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.CompassAxes.Commands;

/// <summary>
/// Command to update an existing compass axis.
/// </summary>
public record UpdateCompassAxisCommand(string Id, string Name, string Description) : ICommand<CompassAxis?>;
