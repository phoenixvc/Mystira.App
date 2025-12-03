namespace Mystira.App.Application.CQRS.EchoTypes.Commands;

/// <summary>
/// Command to delete an echo type.
/// </summary>
public record DeleteEchoTypeCommand(string Id) : ICommand<bool>;
