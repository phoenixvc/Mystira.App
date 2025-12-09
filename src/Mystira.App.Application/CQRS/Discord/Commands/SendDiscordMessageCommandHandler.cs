using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Messaging;

namespace Mystira.App.Application.CQRS.Discord.Commands;

/// <summary>
/// Handler for sending messages to Discord channels.
/// Validates bot connectivity and sends message via the platform-agnostic IChatBotService.
/// </summary>
public class SendDiscordMessageCommandHandler
    : ICommandHandler<SendDiscordMessageCommand, (bool Success, string Message)>
{
    private readonly IChatBotService? _chatBotService;
    private readonly ILogger<SendDiscordMessageCommandHandler> _logger;

    public SendDiscordMessageCommandHandler(
        ILogger<SendDiscordMessageCommandHandler> logger,
        IChatBotService? chatBotService = null)
    {
        _logger = logger;
        _chatBotService = chatBotService;
    }

    public async Task<(bool Success, string Message)> Handle(
        SendDiscordMessageCommand command,
        CancellationToken cancellationToken)
    {
        if (_chatBotService == null)
        {
            _logger.LogWarning("Attempted to send message but chat bot service is not enabled");
            return (false, "Chat bot is not enabled");
        }

        if (!_chatBotService.IsConnected)
        {
            _logger.LogWarning("Attempted to send message but chat bot is not connected");
            return (false, "Chat bot is not connected");
        }

        try
        {
            await _chatBotService.SendMessageAsync(command.ChannelId, command.Message);
            _logger.LogInformation("Successfully sent Discord message to channel {ChannelId}", command.ChannelId);
            return (true, "Message sent successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending Discord message to channel {ChannelId}", command.ChannelId);
            return (false, $"Error sending message: {ex.Message}");
        }
    }
}
