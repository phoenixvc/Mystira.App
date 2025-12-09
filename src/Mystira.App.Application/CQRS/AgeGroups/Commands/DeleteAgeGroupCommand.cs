namespace Mystira.App.Application.CQRS.AgeGroups.Commands;

/// <summary>
/// Command to delete an age group.
/// </summary>
public record DeleteAgeGroupCommand(string Id) : ICommand<bool>;
