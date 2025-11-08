using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Mystira.App.Api.Models;
using Mystira.App.Api.Services;

namespace Mystira.App.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize] // Admin only
public class CharacterController : ControllerBase
{
    private readonly ICharacterMapFileService _characterMapService;
    private readonly ILogger<CharacterController> _logger;

    public CharacterController(ICharacterMapFileService characterMapService, ILogger<CharacterController> logger)
    {
        _characterMapService = characterMapService;
        _logger = logger;
    }

    /// <summary>
    /// Gets a specific character by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Character>> GetCharacter(string id)
    {
        try
        {
            var character = await _characterMapService.GetCharacterAsync(id);
            if (character == null)
            {
                return NotFound(new ErrorResponse 
                { 
                    Message = $"Character not found: {id}",
                    TraceId = HttpContext.TraceIdentifier
                });
            }
            return Ok(character);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting character: {CharacterId}", id);
            return StatusCode(500, new ErrorResponse 
            { 
                Message = "Internal server error while getting character",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Updates an existing character
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<CharacterMapFile>> UpdateCharacter(string id, [FromBody] Character character)
    {
        try
        {
            var updatedCharacterMap = await _characterMapService.UpdateCharacterAsync(id, character);
            return Ok(updatedCharacterMap);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new ErrorResponse 
            { 
                Message = $"Character not found: {id}",
                TraceId = HttpContext.TraceIdentifier
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating character: {CharacterId}", id);
            return StatusCode(500, new ErrorResponse 
            { 
                Message = "Internal server error while updating character",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Removes a character
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult<CharacterMapFile>> DeleteCharacter(string id)
    {
        try
        {
            var updatedCharacterMap = await _characterMapService.RemoveCharacterAsync(id);
            return Ok(updatedCharacterMap);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new ErrorResponse 
            { 
                Message = $"Character not found: {id}",
                TraceId = HttpContext.TraceIdentifier
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting character: {CharacterId}", id);
            return StatusCode(500, new ErrorResponse 
            { 
                Message = "Internal server error while deleting character",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Adds a new character
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<CharacterMapFile>> AddCharacter([FromBody] Character character)
    {
        try
        {
            var updatedCharacterMap = await _characterMapService.AddCharacterAsync(character);
            return Ok(updatedCharacterMap);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ErrorResponse 
            { 
                Message = ex.Message,
                TraceId = HttpContext.TraceIdentifier
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding character: {CharacterId}", character.Id);
            return StatusCode(500, new ErrorResponse 
            { 
                Message = "Internal server error while adding character",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }
}