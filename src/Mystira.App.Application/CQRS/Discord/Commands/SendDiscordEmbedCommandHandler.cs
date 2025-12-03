using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Messaging;

namespace Mystira.App.Application.CQRS.Discord.Commands;

/// <summary>
/// Handler for sending rich embeds to Discord channels.
/// Builds embed from command parameters and sends via Discord bot service.
/// </summary>
public class SendDiscordEmbedCommandHandler
    : ICommandHandler<SendDiscordEmbedCommand, (bool Success, string Message)>
{
    private readonly IDiscordBotService? _discordBotService;
    private readonly ILogger<SendDiscordEmbedCommandHandler> _logger;

    public SendDiscordEmbedCommandHandler(
        ILogger<SendDiscordEmbedCommandHandler> logger,
        IDiscordBotService? discordBotService = null)
    {
        _logger = logger;
        _discordBotService = discordBotService;
    }

    public async Task<(bool Success, string Message)> Handle(
        SendDiscordEmbedCommand command,
        CancellationToken cancellationToken)
    {
        if (_discordBotService == null)
        {
            _logger.LogWarning("Attempted to send Discord embed but bot service is not enabled");
            return (false, "Discord bot is not enabled");
        }

        if (!_discordBotService.IsConnected)
        {
            _logger.LogWarning("Attempted to send Discord embed but bot is not connected");
            return (false, "Discord bot is not connected");
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

            await _discordBotService.SendEmbedAsync(command.ChannelId, embedData);

            _logger.LogInformation("Successfully sent Discord embed to channel {ChannelId}", command.ChannelId);
            return (true, "Embed sent successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending Discord embed to channel {ChannelId}", command.ChannelId);
            return (false, $"Error sending embed: {ex.Message}");
        }
    }
}
