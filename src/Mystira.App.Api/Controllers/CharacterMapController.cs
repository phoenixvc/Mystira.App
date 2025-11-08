using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Mystira.App.Api.Models;
using Mystira.App.Api.Services;

namespace Mystira.App.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize] // Admin only
public class CharacterMapController : ControllerBase
{
    private readonly ICharacterMapFileService _characterMapService;
    private readonly ILogger<CharacterMapController> _logger;

    public CharacterMapController(ICharacterMapFileService characterMapService, ILogger<CharacterMapController> logger)
    {
        _characterMapService = characterMapService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the character map file
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<CharacterMapFile>> GetCharacterMap()
    {
        try
        {
            var characterMap = await _characterMapService.GetCharacterMapFileAsync();
            return Ok(characterMap);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting character map");
            return StatusCode(500, new ErrorResponse 
            { 
                Message = "Internal server error while getting character map",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Updates the character map file
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<CharacterMapFile>> UpdateCharacterMap([FromBody] CharacterMapFile characterMap)
    {
        try
        {
            var updatedCharacterMap = await _characterMapService.UpdateCharacterMapFileAsync(characterMap);
            return Ok(updatedCharacterMap);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating character map");
            return StatusCode(500, new ErrorResponse 
            { 
                Message = "Internal server error while updating character map",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Exports the character map as JSON
    /// </summary>
    [HttpGet("export")]
    public async Task<ActionResult<string>> ExportCharacterMap()
    {
        try
        {
            var jsonData = await _characterMapService.ExportCharacterMapAsync();
            return Ok(jsonData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting character map");
            return StatusCode(500, new ErrorResponse 
            { 
                Message = "Internal server error while exporting character map",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Imports characters from JSON data
    /// </summary>
    [HttpPost("import")]
    public async Task<ActionResult<CharacterMapFile>> ImportCharacterMap([FromBody] string jsonData, [FromQuery] bool overwriteExisting = false)
    {
        try
        {
            var updatedCharacterMap = await _characterMapService.ImportCharacterMapAsync(jsonData, overwriteExisting);
            return Ok(updatedCharacterMap);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ErrorResponse 
            { 
                Message = ex.Message,
                TraceId = HttpContext.TraceIdentifier
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing character map");
            return StatusCode(500, new ErrorResponse 
            { 
                Message = "Internal server error while importing character map",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }
}