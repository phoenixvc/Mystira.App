using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Mystira.App.Domain.Models;
using Mystira.App.Api.Models;
using Mystira.App.Api.Services;

namespace Mystira.App.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class GameSessionsController : ControllerBase
{
    private readonly IGameSessionApiService _sessionService;
    private readonly IAccountApiService _accountService;
    private readonly ILogger<GameSessionsController> _logger;

    public GameSessionsController(
        IGameSessionApiService sessionService, 
        IAccountApiService accountService,
        ILogger<GameSessionsController> logger)
    {
        _sessionService = sessionService;
        _accountService = accountService;
        _logger = logger;
    }

    /// <summary>
    /// Get a specific game session
    /// </summary>
    [HttpGet("{id}")]
    [Authorize] // Requires DM authentication
    public async Task<ActionResult<GameSession>> GetSession(string id)
    {
        try
        {
            var session = await _sessionService.GetSessionAsync(id);
            if (session == null)
            {
                return NotFound(new ErrorResponse 
                { 
                    Message = $"Session not found: {id}",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            return Ok(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting session {SessionId}", id);
            return StatusCode(500, new ErrorResponse 
            { 
                Message = "Internal server error while fetching session",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Get all sessions for a specific account
    /// </summary>
    [HttpGet("account/{accountId}")]
    [Authorize] // Requires authentication
    public async Task<ActionResult<List<GameSessionResponse>>> GetSessionsByAccount(string accountId)
    {
        try
        {
            var sessions = await _sessionService.GetSessionsByAccountAsync(accountId);
            return Ok(sessions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sessions for account {AccountId}", accountId);
            return StatusCode(500, new ErrorResponse 
            { 
                Message = "Internal server error while fetching account sessions",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Get all sessions for a specific profile
    /// </summary>
    [HttpGet("profile/{profileId}")]
    [Authorize] // Requires authentication
    public async Task<ActionResult<List<GameSessionResponse>>> GetSessionsByProfile(string profileId)
    {
        try
        {
            var sessions = await _sessionService.GetSessionsByProfileAsync(profileId);
            return Ok(sessions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sessions for profile {ProfileId}", profileId);
            return StatusCode(500, new ErrorResponse 
            { 
                Message = "Internal server error while fetching profile sessions",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Get session statistics and analytics
    /// </summary>
    [HttpGet("{id}/stats")]
    [Authorize] // Requires authentication
    public async Task<ActionResult<SessionStatsResponse>> GetSessionStats(string id)
    {
        try
        {
            var stats = await _sessionService.GetSessionStatsAsync(id);
            if (stats == null)
            {
                return NotFound(new ErrorResponse 
                { 
                    Message = $"Session not found: {id}",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting session stats {SessionId}", id);
            return StatusCode(500, new ErrorResponse 
            { 
                Message = "Internal server error while fetching session stats",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Check for new achievements in a session
    /// </summary>
    [HttpGet("{id}/achievements")]
    public async Task<ActionResult<List<SessionAchievement>>> GetAchievements(string id)
    {
        try
        {
            var achievements = await _sessionService.CheckAchievementsAsync(id);
            return Ok(achievements);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting achievements for session {SessionId}", id);
            return StatusCode(500, new ErrorResponse 
            { 
                Message = "Internal server error while checking achievements",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }
}