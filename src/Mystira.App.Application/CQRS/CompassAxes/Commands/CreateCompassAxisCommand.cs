using Mystira.Domain.Models;

namespace Mystira.App.Application.CQRS.CompassAxes.Commands;

/// <summary>
/// Command to create a new compass axis.
/// </summary>
public record CreateCompassAxisCommand(string Name, string Description) : ICommand<CompassAxis>;
