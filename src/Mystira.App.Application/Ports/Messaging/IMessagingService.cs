namespace Mystira.App.Application.Ports.Messaging;

/// <summary>
/// Port interface for messaging/notification operations (platform-agnostic).
/// Implementations can use Discord, Slack, Teams, email, SMS, etc.
/// </summary>
public interface IMessagingService
{
    /// <summary>
    /// Start the messaging service connection
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stop the messaging service connection
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Send a simple text message to a specific channel/destination
    /// </summary>
    /// <param name="channelId">Channel/destination identifier</param>
    /// <param name="message">Message content</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SendMessageAsync(ulong channelId, string message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reply to a specific message
    /// </summary>
    /// <param name="messageId">Message ID to reply to</param>
    /// <param name="channelId">Channel ID where the message is located</param>
    /// <param name="reply">Reply content</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ReplyToMessageAsync(ulong messageId, ulong channelId, string reply, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if the service is connected and ready
    /// </summary>
    bool IsConnected { get; }
}
