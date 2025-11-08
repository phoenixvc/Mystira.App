using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Mystira.App.Domain.Models;
using Mystira.App.Api.Models;
using Mystira.App.Api.Services;

namespace Mystira.App.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CharacterMapsController : ControllerBase
{
    private readonly ICharacterMapApiService _characterMapService;
    private readonly ILogger<CharacterMapsController> _logger;

    public CharacterMapsController(ICharacterMapApiService characterMapService, ILogger<CharacterMapsController> logger)
    {
        _characterMapService = characterMapService;
        _logger = logger;
    }

    /// <summary>
    /// Get all character maps
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<CharacterMap>>> GetAllCharacterMaps()
    {
        try
        {
            var characterMaps = await _characterMapService.GetAllCharacterMapsAsync();
            return Ok(characterMaps);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all character maps");
            return StatusCode(500, new ErrorResponse 
            { 
                Message = "Internal server error while fetching character maps",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Get character map by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<CharacterMap>> GetCharacterMap(string id)
    {
        try
        {
            var characterMap = await _characterMapService.GetCharacterMapAsync(id);
            if (characterMap == null)
            {
                return NotFound(new ErrorResponse 
                { 
                    Message = $"Character map not found: {id}",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            return Ok(characterMap);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting character map {Id}", id);
            return StatusCode(500, new ErrorResponse 
            { 
                Message = "Internal server error while fetching character map",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Create a new character map
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<CharacterMap>> CreateCharacterMap([FromBody] CreateCharacterMapRequest request)
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

            var characterMap = await _characterMapService.CreateCharacterMapAsync(request);
            return CreatedAtAction(nameof(GetCharacterMap), new { id = characterMap.Id }, characterMap);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error creating character map");
            return BadRequest(new ErrorResponse 
            { 
                Message = ex.Message,
                TraceId = HttpContext.TraceIdentifier
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating character map");
            return StatusCode(500, new ErrorResponse 
            { 
                Message = "Internal server error while creating character map",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Update a character map
    /// </summary>
    [HttpPut("{id}")]
    [Authorize]
    public async Task<ActionResult<CharacterMap>> UpdateCharacterMap(string id, [FromBody] UpdateCharacterMapRequest request)
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

            var characterMap = await _characterMapService.UpdateCharacterMapAsync(id, request);
            if (characterMap == null)
            {
                return NotFound(new ErrorResponse 
                { 
                    Message = $"Character map not found: {id}",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            return Ok(characterMap);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error updating character map {Id}", id);
            return BadRequest(new ErrorResponse 
            { 
                Message = ex.Message,
                TraceId = HttpContext.TraceIdentifier
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating character map {Id}", id);
            return StatusCode(500, new ErrorResponse 
            { 
                Message = "Internal server error while updating character map",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Delete a character map
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<ActionResult> DeleteCharacterMap(string id)
    {
        try
        {
            var deleted = await _characterMapService.DeleteCharacterMapAsync(id);
            if (!deleted)
            {
                return NotFound(new ErrorResponse 
                { 
                    Message = $"Character map not found: {id}",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting character map {Id}", id);
            return StatusCode(500, new ErrorResponse 
            { 
                Message = "Internal server error while deleting character map",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Export character maps as YAML
    /// </summary>
    [HttpGet("export")]
    [Authorize]
    public async Task<ActionResult> ExportCharacterMaps()
    {
        try
        {
            var yamlContent = await _characterMapService.ExportCharacterMapsAsYamlAsync();
            return File(System.Text.Encoding.UTF8.GetBytes(yamlContent), "application/x-yaml", "character_maps.yaml");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting character maps");
            return StatusCode(500, new ErrorResponse 
            { 
                Message = "Internal server error while exporting character maps",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Import character maps from YAML
    /// </summary>
    [HttpPost("import")]
    [Authorize]
    public async Task<ActionResult<List<CharacterMap>>> ImportCharacterMaps([FromForm] IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new ErrorResponse 
                { 
                    Message = "No file provided",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            if (!file.FileName.EndsWith(".yaml") && !file.FileName.EndsWith(".yml"))
            {
                return BadRequest(new ErrorResponse 
                { 
                    Message = "File must be a YAML file (.yaml or .yml)",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            using var stream = file.OpenReadStream();
            var characterMaps = await _characterMapService.ImportCharacterMapsFromYamlAsync(stream);
            return Ok(characterMaps);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error importing character maps");
            return BadRequest(new ErrorResponse 
            { 
                Message = ex.Message,
                TraceId = HttpContext.TraceIdentifier
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing character maps");
            return StatusCode(500, new ErrorResponse 
            { 
                Message = "Internal server error while importing character maps",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }
}