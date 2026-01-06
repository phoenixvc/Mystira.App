using Microsoft.Extensions.Logging;
using Mystira.App.Application.CQRS.Common.Responses;
using Mystira.App.Application.Ports.Messaging;

namespace Mystira.App.Application.CQRS.Discord.Commands;

/// <summary>
/// Handler for sending messages to Discord channels.
/// Validates bot connectivity and sends message via the platform-agnostic IChatBotService.
/// </summary>
public class SendDiscordMessageCommandHandler
    : ICommandHandler<SendDiscordMessageCommand, CommandResponse>
{
    // FIX: Remove optional dependency anti-pattern - require the service
    private readonly IChatBotService _chatBotService;
    private readonly ILogger<SendDiscordMessageCommandHandler> _logger;

    public SendDiscordMessageCommandHandler(
        ILogger<SendDiscordMessageCommandHandler> logger,
        IChatBotService chatBotService)
    {
        _logger = logger;
        _chatBotService = chatBotService ?? throw new ArgumentNullException(nameof(chatBotService));
    }

    public async Task<CommandResponse> Handle(
        SendDiscordMessageCommand command,
        CancellationToken cancellationToken)
    {
        if (!_chatBotService.IsConnected)
        {
            _logger.LogWarning("Attempted to send message but chat bot is not connected");
            return new CommandResponse(false, "Chat bot is not connected");
        }

        try
        {
            await _chatBotService.SendMessageAsync(command.ChannelId, command.Message, cancellationToken);
            _logger.LogInformation("Successfully sent Discord message to channel {ChannelId}", command.ChannelId);
            return new CommandResponse(true, "Message sent successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending Discord message to channel {ChannelId}", command.ChannelId);
            return new CommandResponse(false, $"Error sending message: {ex.Message}");
        }
    }
}
