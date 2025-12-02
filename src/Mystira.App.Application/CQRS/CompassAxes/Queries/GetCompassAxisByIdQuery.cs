using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.CompassAxes.Queries;

/// <summary>
/// Query to retrieve a compass axis by ID.
/// </summary>
public record GetCompassAxisByIdQuery(string Id) : IQuery<CompassAxis?>;
