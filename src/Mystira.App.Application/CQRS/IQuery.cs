namespace Mystira.App.Application.CQRS;

/// <summary>
/// Marker interface for queries (always return data).
/// Used with Wolverine's convention-based handler discovery.
/// </summary>
/// <typeparam name="TResponse">The type of result returned by the query.</typeparam>
public interface IQuery<out TResponse> { }
