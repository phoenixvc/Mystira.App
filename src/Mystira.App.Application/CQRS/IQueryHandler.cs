namespace Mystira.App.Application.CQRS;

/// <summary>
/// Interface for query handlers.
/// Note: Wolverine uses convention-based discovery, so this interface is optional.
/// It can be used for explicit handler registration or dependency injection scenarios.
/// </summary>
/// <typeparam name="TQuery">The query type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public interface IQueryHandler<in TQuery, TResponse> where TQuery : IQuery<TResponse>
{
    Task<TResponse> Handle(TQuery request, CancellationToken cancellationToken = default);
}
