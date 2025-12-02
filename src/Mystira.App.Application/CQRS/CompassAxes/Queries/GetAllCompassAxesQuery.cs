using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.CompassAxes.Queries;

/// <summary>
/// Query to retrieve all compass axes.
/// </summary>
public record GetAllCompassAxesQuery : IQuery<List<CompassAxis>>;
