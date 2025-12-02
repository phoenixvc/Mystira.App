using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.EchoTypes.Queries;

/// <summary>
/// Query to retrieve an echo type by ID.
/// </summary>
public record GetEchoTypeByIdQuery(string Id) : IQuery<EchoTypeDefinition?>;
