using Discord;
using Microsoft.Extensions.Logging;
using Mystira.App.Application.Interfaces;
using Mystira.App.Infrastructure.Discord.Services;

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
            // Build Discord embed
            var embedBuilder = new EmbedBuilder()
                .WithTitle(command.Title)
                .WithDescription(command.Description)
                .WithColor(new Color(command.ColorRed, command.ColorGreen, command.ColorBlue))
                .WithTimestamp(DateTimeOffset.Now);

            if (!string.IsNullOrEmpty(command.Footer))
            {
                embedBuilder.WithFooter(command.Footer);
            }

            if (command.Fields != null)
            {
                foreach (var field in command.Fields)
                {
                    embedBuilder.AddField(field.Name, field.Value, field.Inline);
                }
            }

            var embed = embedBuilder.Build();
            await _discordBotService.SendEmbedAsync(command.ChannelId, embed);

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
