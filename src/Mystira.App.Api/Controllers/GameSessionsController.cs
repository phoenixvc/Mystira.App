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
    /// Get count of active game sessions
    /// </summary>
    [HttpGet("active/count")]
    public async Task<ActionResult<object>> GetActiveSessionsCount()
    {
        try
        {
            var count = await _sessionService.GetActiveSessionsCountAsync();
            return Ok(new { count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active sessions count");
            return StatusCode(500, new ErrorResponse 
            { 
                Message = "Internal server error while getting active sessions count",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Start a new game session
    /// </summary>
    [HttpPost]
    [Authorize] // Requires authentication
    public async Task<ActionResult<GameSession>> StartSession([FromBody] StartGameSessionRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ValidationErrorResponse
                {
                    Message = "Validation failed",
                    ValidationErrors = ModelState.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToList() ?? new List<string>()
                    ),
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            var accountId = User.FindFirst("sub")?.Value 
                ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
                ?? User.FindFirst("account_id")?.Value;

            if (string.IsNullOrEmpty(accountId))
            {
                return Unauthorized(new ErrorResponse 
                { 
                    Message = "Account ID not found in authentication claims",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            var session = await _sessionService.StartSessionAsync(request, accountId);
            return CreatedAtAction(nameof(GetSession), new { id = session.Id }, session);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error starting session");
            return BadRequest(new ErrorResponse 
            { 
                Message = ex.Message,
                TraceId = HttpContext.TraceIdentifier
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting session");
            return StatusCode(500, new ErrorResponse 
            { 
                Message = "Internal server error while starting session",
                TraceId = HttpContext.TraceIdentifier
            });
        }
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
            var requestingAccountId = User.FindFirst("sub")?.Value 
                ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
                ?? User.FindFirst("account_id")?.Value;

            if (string.IsNullOrEmpty(requestingAccountId))
            {
                return Unauthorized(new ErrorResponse 
                { 
                    Message = "Account ID not found in authentication claims",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            if (requestingAccountId != accountId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

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
    /// Make a choice in a game session
    /// </summary>
    [HttpPost("choice")]
    [Authorize] // Requires DM authentication
    public async Task<ActionResult<GameSession>> MakeChoice([FromBody] MakeChoiceRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ValidationErrorResponse
                {
                    Message = "Validation failed",
                    ValidationErrors = ModelState.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToList() ?? new List<string>()
                    ),
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            var session = await _sessionService.MakeChoiceAsync(request);
            if (session == null)
            {
                return NotFound(new ErrorResponse 
                { 
                    Message = $"Session not found: {request.SessionId}",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            return Ok(session);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation making choice in session {SessionId}", request.SessionId);
            return BadRequest(new ErrorResponse 
            { 
                Message = ex.Message,
                TraceId = HttpContext.TraceIdentifier
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument making choice in session {SessionId}", request.SessionId);
            return BadRequest(new ErrorResponse 
            { 
                Message = ex.Message,
                TraceId = HttpContext.TraceIdentifier
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error making choice in session {SessionId}", request.SessionId);
            return StatusCode(500, new ErrorResponse 
            { 
                Message = "Internal server error while making choice",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Pause a game session
    /// </summary>
    [HttpPost("{id}/pause")]
    [Authorize] // Requires DM authentication
    public async Task<ActionResult<GameSession>> PauseSession(string id)
    {
        try
        {
            var session = await _sessionService.PauseSessionAsync(id);
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
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation pausing session {SessionId}", id);
            return BadRequest(new ErrorResponse 
            { 
                Message = ex.Message,
                TraceId = HttpContext.TraceIdentifier
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pausing session {SessionId}", id);
            return StatusCode(500, new ErrorResponse 
            { 
                Message = "Internal server error while pausing session",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Resume a paused game session
    /// </summary>
    [HttpPost("{id}/resume")]
    [Authorize] // Requires DM authentication
    public async Task<ActionResult<GameSession>> ResumeSession(string id)
    {
        try
        {
            var session = await _sessionService.ResumeSessionAsync(id);
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
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation resuming session {SessionId}", id);
            return BadRequest(new ErrorResponse 
            { 
                Message = ex.Message,
                TraceId = HttpContext.TraceIdentifier
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resuming session {SessionId}", id);
            return StatusCode(500, new ErrorResponse 
            { 
                Message = "Internal server error while resuming session",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// End a game session
    /// </summary>
    [HttpPost("{id}/end")]
    [Authorize] // Requires DM authentication
    public async Task<ActionResult<GameSession>> EndSession(string id)
    {
        try
        {
            var session = await _sessionService.EndSessionAsync(id);
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
            _logger.LogError(ex, "Error ending session {SessionId}", id);
            return StatusCode(500, new ErrorResponse 
            { 
                Message = "Internal server error while ending session",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Get session statistics and analytics
    /// </summary>
    [HttpGet("{id}/stats")]
    [Authorize] // Requires DM authentication
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
    [Authorize] // Requires DM authentication
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

    /// <summary>
    /// Delete a game session (COPPA compliance)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize] // Requires DM authentication
    public async Task<ActionResult> DeleteSession(string id)
    {
        try
        {
            var deleted = await _sessionService.DeleteSessionAsync(id);
            if (!deleted)
            {
                return NotFound(new ErrorResponse 
                { 
                    Message = $"Session not found: {id}",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting session {SessionId}", id);
            return StatusCode(500, new ErrorResponse 
            { 
                Message = "Internal server error while deleting session",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Select a character for the game session
    /// </summary>
    [HttpPost("{id}/select-character")]
    [Authorize] // Requires DM authentication
    public async Task<ActionResult<GameSession>> SelectCharacter(string id, [FromBody] SelectCharacterRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ValidationErrorResponse
                {
                    Message = "Validation failed",
                    ValidationErrors = ModelState.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToList() ?? new List<string>()
                    ),
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            var session = await _sessionService.SelectCharacterAsync(id, request.CharacterId);
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
            _logger.LogError(ex, "Error selecting character for session {SessionId}", id);
            return StatusCode(500, new ErrorResponse 
            { 
                Message = "Internal server error while selecting character",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Get all game sessions for profiles belonging to an account (identified by email)
    /// </summary>
    [HttpGet("account/{email}")]
    public async Task<ActionResult<List<GameSession>>> GetSessionsForAccount(string email)
    {
        try
        {
            var account = await _accountService.GetAccountByEmailAsync(email);
            if (account == null)
            {
                return NotFound(new ErrorResponse 
                { 
                    Message = $"Account with email {email} not found",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            var allSessions = new List<GameSession>();
            var profiles = await _accountService.GetUserProfilesForAccountAsync(account.Id);

            foreach (var profile in profiles)
            {
                var sessions = await _sessionService.GetSessionsForProfileAsync(profile.Id);
                allSessions.AddRange(sessions);
            }

            // Remove duplicates (in case a session involves multiple profiles from the same account)
            var uniqueSessions = allSessions
                .GroupBy(s => s.Id)
                .Select(g => g.First())
                .OrderByDescending(s => s.StartTime)
                .ToList();

            return Ok(uniqueSessions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sessions for account {Email}", email);
            return StatusCode(500, new ErrorResponse 
            { 
                Message = "Internal server error while getting account sessions",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Get session history/statistics for an account (identified by email)
    /// </summary>
    [HttpGet("account/{email}/history")]
    public async Task<ActionResult<object>> GetSessionHistoryForAccount(string email)
    {
        try
        {
            var account = await _accountService.GetAccountByEmailAsync(email);
            if (account == null)
            {
                return NotFound(new ErrorResponse 
                { 
                    Message = $"Account with email {email} not found",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            var allSessions = new List<GameSession>();
            var profiles = await _accountService.GetUserProfilesForAccountAsync(account.Id);

            foreach (var profile in profiles)
            {
                var sessions = await _sessionService.GetSessionsForProfileAsync(profile.Id);
                allSessions.AddRange(sessions);
            }

            // Remove duplicates and calculate statistics
            var uniqueSessions = allSessions
                .GroupBy(s => s.Id)
                .Select(g => g.First())
                .ToList();

            var history = new
            {
                TotalSessions = uniqueSessions.Count,
                CompletedSessions = uniqueSessions.Count(s => s.Status == SessionStatus.Completed),
                TotalPlaytime = uniqueSessions.Sum(s => s.ElapsedTime.TotalMinutes),
                FavoriteScenarios = uniqueSessions
                    .GroupBy(s => s.ScenarioId)
                    .OrderByDescending(g => g.Count())
                    .Take(5)
                    .Select(g => new { ScenarioId = g.Key, PlayCount = g.Count() })
                    .ToList(),
                RecentSessions = uniqueSessions
                    .OrderByDescending(s => s.StartTime)
                    .Take(10)
                    .Select(s => new { 
                        Id = s.Id, 
                        ScenarioId = s.ScenarioId, 
                        StartTime = s.StartTime, 
                        IsCompleted = s.Status == SessionStatus.Completed,
                        Duration = s.ElapsedTime
                    })
                    .ToList()
            };

            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting session history for account {Email}", email);
            return StatusCode(500, new ErrorResponse 
            { 
                Message = "Internal server error while getting account session history",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }
}