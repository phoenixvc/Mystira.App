namespace Mystira.App.Application.CQRS;

/// <summary>
/// Marker interface for commands (write operations) that return a result.
/// Commands modify state and should be idempotent when possible.
/// Used with Wolverine for message-based CQRS pattern.
/// </summary>
/// <typeparam name="TResponse">The type of response returned by the command</typeparam>
public interface ICommand<out TResponse>
{
}

/// <summary>
/// Marker interface for commands (write operations) that don't return a result.
/// Commands modify state and should be idempotent when possible.
/// Used with Wolverine for message-based CQRS pattern.
/// </summary>
public interface ICommand
{
}
