using Mystira.Domain.Models;

namespace Mystira.App.Application.CQRS.EchoTypes.Commands;

/// <summary>
/// Command to create a new echo type.
/// </summary>
public record CreateEchoTypeCommand(string Name, string Description) : ICommand<EchoTypeDefinition>;
