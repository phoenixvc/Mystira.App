using Wolverine;
using Microsoft.AspNetCore.Mvc;
using Mystira.App.Application.CQRS.Archetypes.Queries;
using Mystira.App.Api.Models;
using Mystira.App.Domain.Models;

namespace Mystira.App.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ArchetypesController : ControllerBase
{
    private readonly IMessageBus _bus;
    private readonly ILogger<ArchetypesController> _logger;

    public ArchetypesController(IMessageBus bus, ILogger<ArchetypesController> logger)
    {
        _bus = bus;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<ArchetypeDefinition>>> GetAllArchetypes()
    {
        _logger.LogInformation("GET: Retrieving all archetypes");
        var archetypes = await _bus.InvokeAsync<List<ArchetypeDefinition>>(new GetAllArchetypesQuery());
        return Ok(archetypes);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ArchetypeDefinition>> GetArchetypeById(string id)
    {
        _logger.LogInformation("GET: Retrieving archetype with id: {Id}", id);
        var archetype = await _bus.InvokeAsync<ArchetypeDefinition?>(new GetArchetypeByIdQuery(id));
        if (archetype == null)
        {
            _logger.LogWarning("Archetype with id {Id} not found", id);
            return NotFound(new { message = "Archetype not found" });
        }
        return Ok(archetype);
    }

    [HttpPost("validate")]
    public async Task<ActionResult<ValidationResult>> ValidateArchetype([FromBody] ValidateArchetypeRequest request)
    {
        _logger.LogInformation("POST: Validating archetype: {Name}", request.Name);

        var isValid = await _bus.InvokeAsync<bool>(new ValidateArchetypeQuery(request.Name));
        return Ok(new ValidationResult { IsValid = isValid });
    }
}

public class ValidateArchetypeRequest
{
    public string Name { get; set; } = string.Empty;
}
