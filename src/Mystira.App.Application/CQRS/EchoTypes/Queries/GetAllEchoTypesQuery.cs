using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.EchoTypes.Queries;

/// <summary>
/// Query to retrieve all echo types.
/// </summary>
public record GetAllEchoTypesQuery : IQuery<List<EchoTypeDefinition>>;
