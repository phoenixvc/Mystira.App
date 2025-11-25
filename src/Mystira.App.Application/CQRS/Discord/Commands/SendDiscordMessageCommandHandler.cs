using Microsoft.Extensions.Logging;
using Mystira.App.Application.Interfaces;
using Mystira.App.Infrastructure.Discord.Services;

namespace Mystira.App.Application.CQRS.Discord.Commands;

/// <summary>
/// Handler for sending messages to Discord channels.
/// Validates bot connectivity and sends message via Discord bot service.
/// </summary>
public class SendDiscordMessageCommandHandler
    : ICommandHandler<SendDiscordMessageCommand, (bool Success, string Message)>
{
    private readonly IDiscordBotService? _discordBotService;
    private readonly ILogger<SendDiscordMessageCommandHandler> _logger;

    public SendDiscordMessageCommandHandler(
        ILogger<SendDiscordMessageCommandHandler> logger,
        IDiscordBotService? discordBotService = null)
    {
        _logger = logger;
        _discordBotService = discordBotService;
    }

    public async Task<(bool Success, string Message)> Handle(
        SendDiscordMessageCommand command,
        CancellationToken cancellationToken)
    {
        if (_discordBotService == null)
        {
            _logger.LogWarning("Attempted to send Discord message but bot service is not enabled");
            return (false, "Discord bot is not enabled");
        }

        if (!_discordBotService.IsConnected)
        {
            _logger.LogWarning("Attempted to send Discord message but bot is not connected");
            return (false, "Discord bot is not connected");
        }

        try
        {
            await _discordBotService.SendMessageAsync(command.ChannelId, command.Message);
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
