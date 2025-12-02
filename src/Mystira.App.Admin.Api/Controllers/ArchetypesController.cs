using Microsoft.AspNetCore.Mvc;
using Mystira.App.Admin.Api.Services;
using Mystira.App.Domain.Models;

namespace Mystira.App.Admin.Api.Controllers;

[ApiController]
[Route("api/admin/[controller]")]
public class ArchetypesController : ControllerBase
{
    private readonly IArchetypeApiService _archetypeService;
    private readonly ILogger<ArchetypesController> _logger;

    public ArchetypesController(IArchetypeApiService archetypeService, ILogger<ArchetypesController> logger)
    {
        _archetypeService = archetypeService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<ArchetypeDefinition>>> GetAllArchetypes()
    {
        _logger.LogInformation("GET: Retrieving all archetypes");
        var archetypes = await _archetypeService.GetAllArchetypesAsync();
        return Ok(archetypes);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ArchetypeDefinition>> GetArchetypeById(string id)
    {
        _logger.LogInformation("GET: Retrieving archetype with id: {Id}", id);
        var archetype = await _archetypeService.GetArchetypeByIdAsync(id);
        if (archetype == null)
        {
            _logger.LogWarning("Archetype with id {Id} not found", id);
            return NotFound(new { message = "Archetype not found" });
        }
        return Ok(archetype);
    }

    [HttpPost]
    public async Task<ActionResult<ArchetypeDefinition>> CreateArchetype([FromBody] CreateArchetypeRequest request)
    {
        _logger.LogInformation("POST: Creating archetype with name: {Name}", request.Name);
        
        var archetype = new ArchetypeDefinition
        {
            Name = request.Name,
            Description = request.Description
        };

        var created = await _archetypeService.CreateArchetypeAsync(archetype);
        return CreatedAtAction(nameof(GetArchetypeById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ArchetypeDefinition>> UpdateArchetype(string id, [FromBody] UpdateArchetypeRequest request)
    {
        _logger.LogInformation("PUT: Updating archetype with id: {Id}", id);
        
        var archetype = new ArchetypeDefinition
        {
            Name = request.Name,
            Description = request.Description
        };

        var updated = await _archetypeService.UpdateArchetypeAsync(id, archetype);
        if (updated == null)
        {
            _logger.LogWarning("Archetype with id {Id} not found", id);
            return NotFound(new { message = "Archetype not found" });
        }
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteArchetype(string id)
    {
        _logger.LogInformation("DELETE: Deleting archetype with id: {Id}", id);
        
        var success = await _archetypeService.DeleteArchetypeAsync(id);
        if (!success)
        {
            _logger.LogWarning("Archetype with id {Id} not found", id);
            return NotFound(new { message = "Archetype not found" });
        }
        return NoContent();
    }
}

public class CreateArchetypeRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class UpdateArchetypeRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
