namespace Mystira.App.Application.CQRS.Archetypes.Commands;

/// <summary>
/// Command to delete an archetype.
/// </summary>
public record DeleteArchetypeCommand(string Id) : ICommand<bool>;
