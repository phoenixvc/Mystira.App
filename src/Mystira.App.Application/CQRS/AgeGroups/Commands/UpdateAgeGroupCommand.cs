using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.AgeGroups.Commands;

/// <summary>
/// Command to update an existing age group.
/// </summary>
public record UpdateAgeGroupCommand(string Id, string Name, string Value, int MinimumAge, int MaximumAge, string Description) : ICommand<AgeGroupDefinition?>;
