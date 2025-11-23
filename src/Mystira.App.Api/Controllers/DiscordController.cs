using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mystira.App.Infrastructure.Discord.Services;
using Discord;

namespace Mystira.App.Api.Controllers;

/// <summary>
/// Controller for Discord bot operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class DiscordController : ControllerBase
{
    private readonly IDiscordBotService? _discordBotService;
    private readonly ILogger<DiscordController> _logger;
    private readonly IConfiguration _configuration;

    public DiscordController(
        ILogger<DiscordController> logger,
        IConfiguration configuration,
        IDiscordBotService? discordBotService = null)
    {
        _logger = logger;
        _configuration = configuration;
        _discordBotService = discordBotService;
    }

    /// <summary>
    /// Get Discord bot status
    /// </summary>
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        var enabled = _configuration.GetValue<bool>("Discord:Enabled", false);
        
        if (!enabled || _discordBotService == null)
        {
            return Ok(new
            {
                enabled = false,
                connected = false,
                message = "Discord bot integration is disabled"
            });
        }

        return Ok(new
        {
            enabled = true,
            connected = _discordBotService.IsConnected,
            botUsername = _discordBotService.CurrentUser?.Username,
            botId = _discordBotService.CurrentUser?.Id.ToString()
        });
    }

    /// <summary>
    /// Send a message to a Discord channel
    /// </summary>
    [HttpPost("send")]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
    {
        if (_discordBotService == null)
        {
            return BadRequest(new { message = "Discord bot is not enabled" });
        }

        if (!_discordBotService.IsConnected)
        {
            return ServiceUnavailable(new { message = "Discord bot is not connected" });
        }

        try
        {
            await _discordBotService.SendMessageAsync(request.ChannelId, request.Message);
            
            return Ok(new
            {
                success = true,
                channelId = request.ChannelId,
                message = "Message sent successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending Discord message to channel {ChannelId}", request.ChannelId);
            return StatusCode(500, new { message = $"Error sending message: {ex.Message}" });
        }
    }

    /// <summary>
    /// Send a rich embed to a Discord channel
    /// </summary>
    [HttpPost("send-embed")]
    public async Task<IActionResult> SendEmbed([FromBody] SendEmbedRequest request)
    {
        if (_discordBotService == null)
        {
            return BadRequest(new { message = "Discord bot is not enabled" });
        }

        if (!_discordBotService.IsConnected)
        {
            return ServiceUnavailable(new { message = "Discord bot is not connected" });
        }

        try
        {
            var embedBuilder = new EmbedBuilder()
                .WithTitle(request.Title)
                .WithDescription(request.Description)
                .WithColor(new Color(request.ColorRed, request.ColorGreen, request.ColorBlue))
                .WithTimestamp(DateTimeOffset.Now);

            if (!string.IsNullOrEmpty(request.Footer))
            {
                embedBuilder.WithFooter(request.Footer);
            }

            if (request.Fields != null)
            {
                foreach (var field in request.Fields)
                {
                    embedBuilder.AddField(field.Name, field.Value, field.Inline);
                }
            }

            var embed = embedBuilder.Build();
            await _discordBotService.SendEmbedAsync(request.ChannelId, embed);

            return Ok(new
            {
                success = true,
                channelId = request.ChannelId,
                message = "Embed sent successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending Discord embed to channel {ChannelId}", request.ChannelId);
            return StatusCode(500, new { message = $"Error sending embed: {ex.Message}" });
        }
    }

    private ObjectResult ServiceUnavailable(object value)
    {
        return StatusCode(503, value);
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
