using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mystira.App.Api.Services;
using Mystira.App.Contracts.Requests.CharacterMaps;
using Mystira.App.Contracts.Responses.Common;
using Mystira.App.Domain.Models;

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
}
