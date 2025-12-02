namespace Mystira.App.Application.CQRS.CompassAxes.Queries;

/// <summary>
/// Query to validate if a compass axis name exists.
/// </summary>
public record ValidateCompassAxisQuery(string Name) : IQuery<bool>;
