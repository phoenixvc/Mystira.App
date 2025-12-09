namespace Mystira.App.Application.CQRS.Archetypes.Queries;

/// <summary>
/// Query to validate if an archetype name exists.
/// </summary>
public record ValidateArchetypeQuery(string Name) : IQuery<bool>;
