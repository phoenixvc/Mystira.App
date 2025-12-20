using Microsoft.Extensions.Logging;
using Mystira.App.Application.CQRS.Common.Responses;
using Mystira.App.Application.Ports.Messaging;

namespace Mystira.App.Application.CQRS.Discord.Commands;

/// <summary>
/// Handler for sending rich embeds to Discord channels.
/// Builds embed from command parameters and sends via the platform-agnostic IChatBotService.
/// </summary>
public class SendDiscordEmbedCommandHandler
    : ICommandHandler<SendDiscordEmbedCommand, CommandResponse>
{
    // FIX: Remove optional dependency anti-pattern - require the service
    private readonly IChatBotService _chatBotService;
    private readonly ILogger<SendDiscordEmbedCommandHandler> _logger;

    public SendDiscordEmbedCommandHandler(
        ILogger<SendDiscordEmbedCommandHandler> logger,
        IChatBotService chatBotService)
    {
        _logger = logger;
        _chatBotService = chatBotService ?? throw new ArgumentNullException(nameof(chatBotService));
    }

    public async Task<CommandResponse> Handle(
        SendDiscordEmbedCommand command,
        CancellationToken cancellationToken)
    {
        if (!_chatBotService.IsConnected)
        {
            _logger.LogWarning("Attempted to send embed but chat bot is not connected");
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

            await _chatBotService.SendEmbedAsync(command.ChannelId, embedData, cancellationToken);

            _logger.LogInformation("Successfully sent Discord embed to channel {ChannelId}", command.ChannelId);
            return new CommandResponse(true, "Embed sent successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending Discord embed to channel {ChannelId}", command.ChannelId);
            return new CommandResponse(false, $"Error sending embed: {ex.Message}");
        }
    }
}
