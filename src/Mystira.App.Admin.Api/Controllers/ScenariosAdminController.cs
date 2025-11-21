using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mystira.App.Admin.Api.Models;
using Mystira.App.Admin.Api.Services;
using Mystira.App.Domain.Models;

namespace Mystira.App.Admin.Api.Controllers;

[ApiController]
[Route("api/admin/[controller]")]
[Produces("application/json")]
[Authorize]
public class ScenariosAdminController : ControllerBase
{
    private readonly IScenarioApiService _scenarioService;
    private readonly ILogger<ScenariosAdminController> _logger;

    public ScenariosAdminController(IScenarioApiService scenarioService, ILogger<ScenariosAdminController> logger)
    {
        _scenarioService = scenarioService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new scenario (Admin authentication required)
    /// </summary>
    [HttpPost]
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
    /// Update an existing scenario (Admin authentication required)
    /// </summary>
    [HttpPut("{id}")]
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
    /// Delete a scenario (Admin authentication required)
    /// </summary>
    [HttpDelete("{id}")]
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
    /// Validate a scenario structure
    /// </summary>
    [HttpPost("validate")]
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
