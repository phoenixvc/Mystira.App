using MediatR;
using Microsoft.AspNetCore.Mvc;
using Mystira.App.Application.CQRS.Scenarios.Commands;
using Mystira.App.Application.CQRS.Scenarios.Queries;
using Mystira.App.Contracts.Requests.Scenarios;
using Mystira.App.Contracts.Responses.Common;
using Mystira.App.Contracts.Responses.Scenarios;
using Mystira.App.Domain.Models;

namespace Mystira.App.Api.Controllers;

/// <summary>
/// Controller for scenario management.
/// Follows hexagonal architecture - uses only IMediator (CQRS pattern).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ScenariosController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ScenariosController> _logger;

    public ScenariosController(IMediator mediator, ILogger<ScenariosController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get scenarios with optional filtering
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ScenarioListResponse>> GetScenarios([FromQuery] ScenarioQueryRequest request)
    {
        try
        {
            var query = new GetPaginatedScenariosQuery(
                request.Page,
                request.PageSize,
                request.Search,
                request.AgeGroup,
                request.Genre);

            var result = await _mediator.Send(query);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting scenarios");
            return StatusCode(500, new ErrorResponse
            {
                Message = "Internal server error while fetching scenarios",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Get a specific scenario by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Scenario>> GetScenario(string id)
    {
        try
        {
            var query = new GetScenarioQuery(id);
            var scenario = await _mediator.Send(query);

            if (scenario == null)
            {
                return NotFound(new ErrorResponse
                {
                    Message = $"Scenario not found: {id}",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            return Ok(scenario);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting scenario {ScenarioId}", id);
            return StatusCode(500, new ErrorResponse
            {
                Message = "Internal server error while fetching scenario",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Get scenarios appropriate for a specific age group
    /// </summary>
    [HttpGet("age-group/{ageGroup}")]
    public async Task<ActionResult<List<Scenario>>> GetScenariosByAgeGroup(string ageGroup)
    {
        try
        {
            var query = new GetScenariosByAgeGroupQuery(ageGroup);
            var scenarios = await _mediator.Send(query);
            return Ok(scenarios);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting scenarios for age group {AgeGroup}", ageGroup);
            return StatusCode(500, new ErrorResponse
            {
                Message = "Internal server error while fetching scenarios by age group",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Get featured scenarios for the home page
    /// </summary>
    [HttpGet("featured")]
    public async Task<ActionResult<List<Scenario>>> GetFeaturedScenarios()
    {
        try
        {
            var query = new GetFeaturedScenariosQuery();
            var scenarios = await _mediator.Send(query);
            return Ok(scenarios);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting featured scenarios");
            return StatusCode(500, new ErrorResponse
            {
                Message = "Internal server error while fetching featured scenarios",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Get scenarios with game state for account
    /// </summary>
    [HttpGet("with-game-state/{accountId}")]
    public async Task<ActionResult<ScenarioGameStateResponse>> GetScenariosWithGameState(string accountId)
    {
        try
        {
            _logger.LogInformation("Fetching scenarios with game state for account: {AccountId}", accountId);

            var query = new GetScenariosWithGameStateQuery(accountId);
            var result = await _mediator.Send(query);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting scenarios with game state for account {AccountId}", accountId);
            return StatusCode(500, new ErrorResponse
            {
                Message = "Internal server error while fetching scenarios with game state",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }
}
