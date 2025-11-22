using Microsoft.AspNetCore.Mvc;
using Mystira.App.Admin.Api.Models;
using Mystira.App.Admin.Api.Services;
using Mystira.App.Domain.Models;

namespace Mystira.App.Admin.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ScenariosController : ControllerBase
{
    private readonly IScenarioApiService _scenarioService;
    private readonly ILogger<ScenariosController> _logger;

    public ScenariosController(IScenarioApiService scenarioService, ILogger<ScenariosController> logger)
    {
        _scenarioService = scenarioService;
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
            var result = await _scenarioService.GetScenariosAsync(request);
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
            var scenario = await _scenarioService.GetScenarioByIdAsync(id);
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
            var scenarios = await _scenarioService.GetScenariosByAgeGroupAsync(ageGroup);
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
            var scenarios = await _scenarioService.GetFeaturedScenariosAsync();
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
}
