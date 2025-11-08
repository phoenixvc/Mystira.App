using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Mystira.App.Api.Models;
using Mystira.App.Api.Services;

namespace Mystira.App.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize] // Admin only
public class CharacterMediaMetadataController : ControllerBase
{
    private readonly ICharacterMediaMetadataService _characterMediaMetadataService;
    private readonly ILogger<CharacterMediaMetadataController> _logger;

    public CharacterMediaMetadataController(ICharacterMediaMetadataService characterMediaMetadataService, ILogger<CharacterMediaMetadataController> logger)
    {
        _characterMediaMetadataService = characterMediaMetadataService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the character media metadata file
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<CharacterMediaMetadataFile>> GetCharacterMediaMetadataFile()
    {
        try
        {
            var metadataFile = await _characterMediaMetadataService.GetCharacterMediaMetadataFileAsync();
            return Ok(metadataFile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting character media metadata file");
            return StatusCode(500, new ErrorResponse 
            { 
                Message = "Internal server error while getting character media metadata file",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Updates the character media metadata file
    /// </summary>
    [HttpPut]
    public async Task<ActionResult<CharacterMediaMetadataFile>> UpdateCharacterMediaMetadataFile([FromBody] CharacterMediaMetadataFile metadataFile)
    {
        try
        {
            var updatedFile = await _characterMediaMetadataService.UpdateCharacterMediaMetadataFileAsync(metadataFile);
            return Ok(updatedFile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating character media metadata file");
            return StatusCode(500, new ErrorResponse 
            { 
                Message = "Internal server error while updating character media metadata file",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Gets a specific character media metadata entry
    /// </summary>
    [HttpGet("entries/{entryId}")]
    public async Task<ActionResult<CharacterMediaMetadataEntry>> GetCharacterMediaMetadataEntry(string entryId)
    {
        try
        {
            var entry = await _characterMediaMetadataService.GetCharacterMediaMetadataEntryAsync(entryId);
            if (entry == null)
            {
                return NotFound(new ErrorResponse 
                { 
                    Message = $"Character media metadata entry not found: {entryId}",
                    TraceId = HttpContext.TraceIdentifier
                });
            }
            return Ok(entry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting character media metadata entry: {EntryId}", entryId);
            return StatusCode(500, new ErrorResponse 
            { 
                Message = "Internal server error while getting character media metadata entry",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Adds a new character media metadata entry
    /// </summary>
    [HttpPost("entries")]
    public async Task<ActionResult<CharacterMediaMetadataFile>> AddCharacterMediaMetadataEntry([FromBody] CharacterMediaMetadataEntry entry)
    {
        try
        {
            var updatedFile = await _characterMediaMetadataService.AddCharacterMediaMetadataEntryAsync(entry);
            return Ok(updatedFile);
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
            _logger.LogError(ex, "Error adding character media metadata entry: {EntryId}", entry.Id);
            return StatusCode(500, new ErrorResponse 
            { 
                Message = "Internal server error while adding character media metadata entry",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Updates an existing character media metadata entry
    /// </summary>
    [HttpPut("entries/{entryId}")]
    public async Task<ActionResult<CharacterMediaMetadataFile>> UpdateCharacterMediaMetadataEntry(string entryId, [FromBody] CharacterMediaMetadataEntry entry)
    {
        try
        {
            var updatedFile = await _characterMediaMetadataService.UpdateCharacterMediaMetadataEntryAsync(entryId, entry);
            return Ok(updatedFile);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new ErrorResponse 
            { 
                Message = $"Character media metadata entry not found: {entryId}",
                TraceId = HttpContext.TraceIdentifier
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating character media metadata entry: {EntryId}", entryId);
            return StatusCode(500, new ErrorResponse 
            { 
                Message = "Internal server error while updating character media metadata entry",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Removes a character media metadata entry
    /// </summary>
    [HttpDelete("entries/{entryId}")]
    public async Task<ActionResult<CharacterMediaMetadataFile>> RemoveCharacterMediaMetadataEntry(string entryId)
    {
        try
        {
            var updatedFile = await _characterMediaMetadataService.RemoveCharacterMediaMetadataEntryAsync(entryId);
            return Ok(updatedFile);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new ErrorResponse 
            { 
                Message = $"Character media metadata entry not found: {entryId}",
                TraceId = HttpContext.TraceIdentifier
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing character media metadata entry: {EntryId}", entryId);
            return StatusCode(500, new ErrorResponse 
            { 
                Message = "Internal server error while removing character media metadata entry",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Import character media metadata entries from JSON
    /// </summary>
    [HttpPost("import")]
    public async Task<ActionResult<CharacterMediaMetadataFile>> ImportCharacterMediaMetadataEntries([FromBody] string jsonData, [FromQuery] bool overwriteExisting = false)
    {
        try
        {
            var updatedFile = await _characterMediaMetadataService.ImportCharacterMediaMetadataEntriesAsync(jsonData, overwriteExisting);
            return Ok(updatedFile);
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
            _logger.LogError(ex, "Error importing character media metadata entries");
            return StatusCode(500, new ErrorResponse 
            { 
                Message = "Internal server error while importing character media metadata entries",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }
}