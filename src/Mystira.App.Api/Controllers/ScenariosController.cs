using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Mystira.App.Domain.Models;
using Mystira.App.Api.Models;
using Mystira.App.Api.Services;

namespace Mystira.App.Api.Controllers;

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
    /// Create a new scenario (DM authentication required)
    /// </summary>
    [HttpPost]
    [Authorize] // Requires DM authentication
    public async Task<ActionResult<Scenario>> CreateScenario([FromBody] CreateScenarioRequest request)
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

            var scenario = await _scenarioService.CreateScenarioAsync(request);
            return CreatedAtAction(nameof(GetScenario), new { id = scenario.Id }, scenario);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error creating scenario");
            return BadRequest(new ErrorResponse 
            { 
                Message = ex.Message,
                TraceId = HttpContext.TraceIdentifier
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating scenario");
            return StatusCode(500, new ErrorResponse 
            { 
                Message = "Internal server error while creating scenario",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Update an existing scenario (DM authentication required)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize] // Requires DM authentication
    public async Task<ActionResult<Scenario>> UpdateScenario(string id, [FromBody] CreateScenarioRequest request)
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

            var scenario = await _scenarioService.UpdateScenarioAsync(id, request);
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
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error updating scenario {ScenarioId}", id);
            return BadRequest(new ErrorResponse 
            { 
                Message = ex.Message,
                TraceId = HttpContext.TraceIdentifier
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating scenario {ScenarioId}", id);
            return StatusCode(500, new ErrorResponse 
            { 
                Message = "Internal server error while updating scenario",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Delete a scenario (DM authentication required)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize] // Requires DM authentication
    public async Task<ActionResult> DeleteScenario(string id)
    {
        try
        {
            var deleted = await _scenarioService.DeleteScenarioAsync(id);
            if (!deleted)
            {
                return NotFound(new ErrorResponse 
                { 
                    Message = $"Scenario not found: {id}",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting scenario {ScenarioId}", id);
            return StatusCode(500, new ErrorResponse 
            { 
                Message = "Internal server error while deleting scenario",
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

    /// <summary>
    /// Validate a scenario structure
    /// </summary>
    [HttpPost("validate")]
    [Authorize] // Requires DM authentication
    public async Task<ActionResult<bool>> ValidateScenario([FromBody] Scenario scenario)
    {
        try
        {
            await _scenarioService.ValidateScenarioAsync(scenario);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating scenario");
            return StatusCode(500, new ErrorResponse 
            { 
                Message = "Internal server error while validating scenario",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Validate media and character references for a specific scenario
    /// </summary>
    [HttpGet("{id}/validate-references")]
    public async Task<ActionResult<ScenarioReferenceValidation>> ValidateScenarioReferences(string id, [FromQuery] bool includeMetadataValidation = true)
    {
        try
        {
            var result = await _scenarioService.ValidateScenarioReferencesAsync(id, includeMetadataValidation);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Scenario not found: {ScenarioId}", id);
            return NotFound(new ErrorResponse 
            { 
                Message = ex.Message,
                TraceId = HttpContext.TraceIdentifier
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating scenario references: {ScenarioId}", id);
            return StatusCode(500, new ErrorResponse 
            { 
                Message = "Internal server error while validating scenario references",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Validate media and character references for all scenarios
    /// </summary>
    [HttpGet("validate-all-references")]
    public async Task<ActionResult<List<ScenarioReferenceValidation>>> ValidateAllScenarioReferences([FromQuery] bool includeMetadataValidation = true)
    {
        try
        {
            var result = await _scenarioService.ValidateAllScenarioReferencesAsync(includeMetadataValidation);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating all scenario references");
            return StatusCode(500, new ErrorResponse 
            { 
                Message = "Internal server error while validating all scenario references",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }
}