using Microsoft.AspNetCore.Mvc;
using Mystira.App.Api.Services;
using Mystira.App.Domain.Models;

namespace Mystira.App.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
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

    [HttpPost("validate")]
    public async Task<ActionResult<dynamic>> ValidateArchetype([FromBody] ValidateArchetypeRequest request)
    {
        _logger.LogInformation("POST: Validating archetype: {Name}", request.Name);
        
        var isValid = await _archetypeService.IsValidArchetypeAsync(request.Name);
        return Ok(new { isValid });
    }
}

public class ValidateArchetypeRequest
{
    public string Name { get; set; } = string.Empty;
}
