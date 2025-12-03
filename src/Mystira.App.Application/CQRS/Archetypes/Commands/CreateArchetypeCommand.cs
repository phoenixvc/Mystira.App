using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.Archetypes.Commands;

/// <summary>
/// Command to create a new archetype.
/// </summary>
public record CreateArchetypeCommand(string Name, string Description) : ICommand<ArchetypeDefinition>;
