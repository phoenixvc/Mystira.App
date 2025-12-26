using Microsoft.Extensions.Logging;
using Mystira.App.Application.CQRS.Common.Responses;
using Mystira.App.Application.Ports.Messaging;

namespace Mystira.App.Application.CQRS.Discord.Commands;

/// <summary>
/// Wolverine message handler for sending messages to Discord channels.
/// Validates bot connectivity and sends message via the platform-agnostic IChatBotService.
/// </summary>
public static class SendDiscordMessageCommandHandler
{
    public static async Task<CommandResponse> Handle(
        SendDiscordMessageCommand command,
        IChatBotService chatBotService,
        ILogger<SendDiscordMessageCommand> logger,
        CancellationToken ct)
    {
        if (chatBotService == null)
            throw new ArgumentNullException(nameof(chatBotService));

        if (!chatBotService.IsConnected)
        {
            logger.LogWarning("Attempted to send message but chat bot is not connected");
            return new CommandResponse(false, "Chat bot is not connected");
        }

        try
        {
            await chatBotService.SendMessageAsync(command.ChannelId, command.Message, ct);
            logger.LogInformation("Successfully sent Discord message to channel {ChannelId}", command.ChannelId);
            return new CommandResponse(true, "Message sent successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending Discord message to channel {ChannelId}", command.ChannelId);
            return new CommandResponse(false, $"Error sending message: {ex.Message}");
        }
    }
}
