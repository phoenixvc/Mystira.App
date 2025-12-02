using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.Archetypes.Queries;

/// <summary>
/// Query to retrieve all archetypes.
/// </summary>
public record GetAllArchetypesQuery : IQuery<List<ArchetypeDefinition>>;
