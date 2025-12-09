using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mystira.App.Application.CQRS.Discord.Commands;
using Mystira.App.Application.CQRS.Discord.Queries;

namespace Mystira.App.Api.Controllers;

/// <summary>
/// Controller for Discord bot operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DiscordController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<DiscordController> _logger;

    public DiscordController(
        IMediator mediator,
        ILogger<DiscordController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get Discord bot status
    /// </summary>
    [HttpGet("status")]
    [AllowAnonymous]
    public async Task<IActionResult> GetStatus()
    {
        var query = new GetDiscordBotStatusQuery();
        var status = await _mediator.Send(query);

        return Ok(new
        {
            enabled = status.Enabled,
            connected = status.Connected,
            botUsername = status.BotUsername,
            botId = status.BotId,
            message = status.Message
        });
    }

    /// <summary>
    /// Send a message to a Discord channel
    /// </summary>
    [HttpPost("send")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
    {
        var command = new SendDiscordMessageCommand(request.ChannelId, request.Message);
        var (success, message) = await _mediator.Send(command);

        if (!success)
        {
            if (message.Contains("not enabled"))
            {
                return BadRequest(new { message });
            }
            if (message.Contains("not connected"))
            {
                return StatusCode(503, new { message });
            }
            return StatusCode(500, new { message });
        }

        return Ok(new
        {
            success = true,
            channelId = request.ChannelId,
            message
        });
    }

    /// <summary>
    /// Send a rich embed to a Discord channel
    /// </summary>
    [HttpPost("send-embed")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SendEmbed([FromBody] SendEmbedRequest request)
    {
        // Map request fields to command fields
        var commandFields = request.Fields?
            .Select(f => new DiscordEmbedField(f.Name, f.Value, f.Inline))
            .ToList();

        var command = new SendDiscordEmbedCommand(
            request.ChannelId,
            request.Title,
            request.Description,
            request.ColorRed,
            request.ColorGreen,
            request.ColorBlue,
            request.Footer,
            commandFields);

        var (success, message) = await _mediator.Send(command);

        if (!success)
        {
            if (message.Contains("not enabled"))
            {
                return BadRequest(new { message });
            }
            if (message.Contains("not connected"))
            {
                return StatusCode(503, new { message });
            }
            return StatusCode(500, new { message });
        }

        return Ok(new
        {
            success = true,
            channelId = request.ChannelId,
            message
        });
    }
}

/// <summary>
/// Request to send a simple message
/// </summary>
public class SendMessageRequest
{
    public ulong ChannelId { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Request to send a rich embed
/// </summary>
public class SendEmbedRequest
{
    public ulong ChannelId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public byte ColorRed { get; set; } = 52;
    public byte ColorGreen { get; set; } = 152;
    public byte ColorBlue { get; set; } = 219;
    public string? Footer { get; set; }
    public List<EmbedField>? Fields { get; set; }
}

public class EmbedField
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public bool Inline { get; set; }
}
