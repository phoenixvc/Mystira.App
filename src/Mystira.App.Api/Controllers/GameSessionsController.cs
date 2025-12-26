using Wolverine;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mystira.App.Application.CQRS.Accounts.Commands;
using Mystira.App.Application.CQRS.GameSessions.Commands;
using Mystira.App.Application.CQRS.GameSessions.Queries;
using Mystira.Contracts.App.Requests.GameSessions;
using Mystira.Contracts.App.Requests.Scenarios;
using Mystira.Contracts.App.Responses.GameSessions;
using Mystira.App.Api.Models;
using Mystira.App.Domain.Models;

namespace Mystira.App.Api.Controllers;

/// <summary>
/// Controller for game session management.
/// Follows hexagonal architecture - uses only IMessageBus (CQRS pattern).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class GameSessionsController : ControllerBase
{
    private readonly IMessageBus _bus;
    private readonly ILogger<GameSessionsController> _logger;

    public GameSessionsController(
        IMessageBus bus,
        ILogger<GameSessionsController> logger)
    {
        _bus = bus;
        _logger = logger;
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
                return BadRequest(new ErrorResponse
                {
                    Message = "Validation failed",
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

            var command = new StartGameSessionCommand(request);
            var session = await _bus.InvokeAsync<GameSession>(command);
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
    /// Pause an active game session
    /// </summary>
    [HttpPost("{id}/pause")]
    [Authorize]
    public async Task<ActionResult<GameSession>> PauseSession(string id)
    {
        try
        {
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

            var command = new PauseGameSessionCommand(id);
            var session = await _bus.InvokeAsync<GameSession?>(command);
            if (session == null)
            {
                return NotFound(new ErrorResponse
                {
                    Message = $"Session not found or cannot be paused: {id}",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            return Ok(session);
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
    [Authorize]
    public async Task<ActionResult<GameSession>> ResumeSession(string id)
    {
        try
        {
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

            var command = new ResumeGameSessionCommand(id);
            var session = await _bus.InvokeAsync<GameSession?>(command);
            if (session == null)
            {
                return NotFound(new ErrorResponse
                {
                    Message = $"Session not found or cannot be resumed: {id}",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            return Ok(session);
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
    [Authorize]
    public async Task<ActionResult<GameSession>> EndSession(string id)
    {
        try
        {
            var command = new EndGameSessionCommand(id);
            var session = await _bus.InvokeAsync<GameSession?>(command);
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
    /// Finalize a game session: calculate scoring for eligible players and award badges
    /// </summary>
    [HttpPost("{id}/finalize")]
    [Authorize]
    public async Task<ActionResult<object>> FinalizeSession(string id)
    {
        try
        {
            var command = new FinalizeGameSessionCommand(id);
            var result = await _bus.InvokeAsync<object>(command);

            // Always return 200 with the aggregation result; if session not found, Awards will be empty
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finalizing session {SessionId}", id);
            return StatusCode(500, new ErrorResponse
            {
                Message = "Internal server error while finalizing session",
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

            var query = new GetSessionsByAccountQuery(accountId);
            var sessions = await _bus.InvokeAsync<List<GameSessionResponse>>(query);
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

            var command = new MakeChoiceCommand(request);
            var session = await _bus.InvokeAsync<GameSession?>(command);
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
    /// Get a specific game session
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<GameSession>> GetSession(string id)
    {
        try
        {
            var query = new GetGameSessionQuery(id);
            var session = await _bus.InvokeAsync<GameSession?>(query);
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
    /// Get all sessions for a specific profile
    /// </summary>
    [HttpGet("profile/{profileId}")]
    public async Task<ActionResult<List<GameSessionResponse>>> GetSessionsByProfile(string profileId)
    {
        try
        {
            var query = new GetSessionsByProfileQuery(profileId);
            var sessions = await _bus.InvokeAsync<List<GameSessionResponse>>(query);
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
    /// Get in-progress and paused game sessions for a specific account
    /// </summary>
    [HttpGet("account/{accountId}/in-progress")]
    [Authorize]
    public async Task<ActionResult<List<GameSessionResponse>>> GetInProgressSessions(string accountId)
    {
        try
        {
            var query = new GetInProgressSessionsQuery(accountId);
            var sessions = await _bus.InvokeAsync<List<GameSessionResponse>>(query);
            return Ok(sessions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting in-progress sessions for account {AccountId}", accountId);
            return StatusCode(500, new ErrorResponse
            {
                Message = "Internal server error while fetching in-progress sessions",
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
            var query = new GetSessionStatsQuery(id);
            var stats = await _bus.InvokeAsync<SessionStatsResponse?>(query);
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
            var query = new GetAchievementsQuery(id);
            var achievements = await _bus.InvokeAsync<List<SessionAchievement>>(query);
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
    /// Progress a game session to a new scene
    /// </summary>
    [HttpPost("{id}/progress-scene")]
    public async Task<ActionResult<GameSession>> ProgressScene(string id, [FromBody] ProgressSceneRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ErrorResponse
                {
                    Message = "Validation failed",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            request.SessionId = id;
            var command = new ProgressSceneCommand(request);
            var session = await _bus.InvokeAsync<GameSession?>(command);
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
            _logger.LogWarning(ex, "Invalid operation progressing scene in session {SessionId}", id);
            return BadRequest(new ErrorResponse
            {
                Message = ex.Message,
                TraceId = HttpContext.TraceIdentifier
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument progressing scene in session {SessionId}", id);
            return BadRequest(new ErrorResponse
            {
                Message = ex.Message,
                TraceId = HttpContext.TraceIdentifier
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error progressing scene in session {SessionId}", id);
            return StatusCode(500, new ErrorResponse
            {
                Message = "Internal server error while progressing scene",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Mark a scenario as completed for an account
    /// </summary>
    [HttpPost("complete-scenario")]
    public async Task<ActionResult> CompleteScenarioForAccount([FromBody] CompleteScenarioRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.AccountId) || string.IsNullOrEmpty(request.ScenarioId))
            {
                return BadRequest(new ErrorResponse
                {
                    Message = "AccountId and ScenarioId are required",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            var command = new AddCompletedScenarioCommand(request.AccountId, request.ScenarioId);
            var success = await _bus.InvokeAsync<bool>(command);
            if (!success)
            {
                return NotFound(new ErrorResponse
                {
                    Message = $"Account not found: {request.AccountId}",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing scenario {ScenarioId} for account {AccountId}",
                request.ScenarioId, request.AccountId);
            return StatusCode(500, new ErrorResponse
            {
                Message = "Internal server error while completing scenario",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }
}
