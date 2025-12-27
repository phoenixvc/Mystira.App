namespace Mystira.App.Application.CQRS;

/// <summary>
/// Marker interface for commands that return a result.
/// Used with Wolverine's convention-based handler discovery.
/// </summary>
/// <typeparam name="TResponse">The type of result returned by the command.</typeparam>
public interface ICommand<out TResponse> { }

/// <summary>
/// Marker interface for commands with no result.
/// Used with Wolverine's convention-based handler discovery.
/// </summary>
public interface ICommand { }
