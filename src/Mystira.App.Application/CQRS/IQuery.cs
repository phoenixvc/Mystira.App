using MediatR;

namespace Mystira.App.Application.CQRS;

/// <summary>
/// Marker interface for queries (read operations)
/// Queries should NOT modify state and can be cached
/// </summary>
/// <typeparam name="TResponse">The type of response returned by the query</typeparam>
public interface IQuery<out TResponse> : IRequest<TResponse>
{
}
