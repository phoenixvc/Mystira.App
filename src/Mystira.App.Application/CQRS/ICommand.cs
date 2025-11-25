using MediatR;

namespace Mystira.App.Application.CQRS;

/// <summary>
/// Marker interface for commands (write operations) that return a result
/// Commands modify state and should be idempotent when possible
/// </summary>
/// <typeparam name="TResponse">The type of response returned by the command</typeparam>
public interface ICommand<out TResponse> : IRequest<TResponse>
{
}

/// <summary>
/// Marker interface for commands (write operations) that don't return a result
/// Commands modify state and should be idempotent when possible
/// </summary>
public interface ICommand : IRequest
{
}
