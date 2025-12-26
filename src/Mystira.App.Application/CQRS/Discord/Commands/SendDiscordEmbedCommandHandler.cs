using Microsoft.Extensions.Logging;
using Mystira.App.Application.CQRS.Common.Responses;
using Mystira.App.Application.Ports.Messaging;

namespace Mystira.App.Application.CQRS.Discord.Commands;

/// <summary>
/// Wolverine message handler for sending rich embeds to Discord channels.
/// Builds embed from command parameters and sends via the platform-agnostic IChatBotService.
/// </summary>
public static class SendDiscordEmbedCommandHandler
{
    public static async Task<CommandResponse> Handle(
        SendDiscordEmbedCommand command,
        IChatBotService chatBotService,
        ILogger<SendDiscordEmbedCommand> logger,
        CancellationToken ct)
    {
        if (chatBotService == null)
            throw new ArgumentNullException(nameof(chatBotService));

        if (!chatBotService.IsConnected)
        {
            logger.LogWarning("Attempted to send embed but chat bot is not connected");
            return new CommandResponse(false, "Chat bot is not connected");
        }

        try
        {
            // Build platform-agnostic embed data
            var embedData = new EmbedData
            {
                Title = command.Title,
                Description = command.Description,
                ColorRed = command.ColorRed,
                ColorGreen = command.ColorGreen,
                ColorBlue = command.ColorBlue,
                Footer = command.Footer,
                Fields = command.Fields?.Select(f => new EmbedFieldData
                {
                    Name = f.Name,
                    Value = f.Value,
                    Inline = f.Inline
                }).ToList()
            };

            await chatBotService.SendEmbedAsync(command.ChannelId, embedData, ct);

            logger.LogInformation("Successfully sent Discord embed to channel {ChannelId}", command.ChannelId);
            return new CommandResponse(true, "Embed sent successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending Discord embed to channel {ChannelId}", command.ChannelId);
            return new CommandResponse(false, $"Error sending embed: {ex.Message}");
        }
    }
}
