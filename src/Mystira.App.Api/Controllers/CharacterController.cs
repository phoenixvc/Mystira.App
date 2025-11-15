using Microsoft.AspNetCore.Mvc;
using Mystira.App.Api.Models;
using Mystira.App.Api.Services;

namespace Mystira.App.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
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
}
