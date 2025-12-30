using Mystira.Domain.Models;

namespace Mystira.App.Application.CQRS.Archetypes.Commands;

/// <summary>
/// Command to update an existing archetype.
/// </summary>
public record UpdateArchetypeCommand(string Id, string Name, string Description) : ICommand<ArchetypeDefinition?>;
