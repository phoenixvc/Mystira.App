using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.Archetypes.Queries;

/// <summary>
/// Query to retrieve an archetype by ID.
/// </summary>
public record GetArchetypeByIdQuery(string Id) : IQuery<ArchetypeDefinition?>;
