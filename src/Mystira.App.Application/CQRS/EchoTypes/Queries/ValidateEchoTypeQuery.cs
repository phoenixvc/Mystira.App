namespace Mystira.App.Application.CQRS.EchoTypes.Queries;

/// <summary>
/// Query to validate if an echo type name exists.
/// </summary>
public record ValidateEchoTypeQuery(string Name) : IQuery<bool>;
