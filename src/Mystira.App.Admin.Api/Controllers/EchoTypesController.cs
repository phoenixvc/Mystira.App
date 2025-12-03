using MediatR;
using Microsoft.AspNetCore.Mvc;
using Mystira.App.Application.CQRS.EchoTypes.Commands;
using Mystira.App.Application.CQRS.EchoTypes.Queries;
using Mystira.App.Domain.Models;

namespace Mystira.App.Admin.Api.Controllers;

[ApiController]
[Route("api/admin/[controller]")]
public class EchoTypesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<EchoTypesController> _logger;

    public EchoTypesController(IMediator mediator, ILogger<EchoTypesController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<EchoTypeDefinition>>> GetAllEchoTypes()
    {
        _logger.LogInformation("GET: Retrieving all echo types");
        var echoTypes = await _mediator.Send(new GetAllEchoTypesQuery());
        return Ok(echoTypes);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<EchoTypeDefinition>> GetEchoTypeById(string id)
    {
        _logger.LogInformation("GET: Retrieving echo type with id: {Id}", id);
        var echoType = await _mediator.Send(new GetEchoTypeByIdQuery(id));
        if (echoType == null)
        {
            _logger.LogWarning("Echo type with id {Id} not found", id);
            return NotFound(new { message = "Echo type not found" });
        }
        return Ok(echoType);
    }

    [HttpPost]
    public async Task<ActionResult<EchoTypeDefinition>> CreateEchoType([FromBody] CreateEchoTypeRequest request)
    {
        _logger.LogInformation("POST: Creating echo type with name: {Name}", request.Name);
        
        var created = await _mediator.Send(new CreateEchoTypeCommand(request.Name, request.Description));
        return CreatedAtAction(nameof(GetEchoTypeById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<EchoTypeDefinition>> UpdateEchoType(string id, [FromBody] UpdateEchoTypeRequest request)
    {
        _logger.LogInformation("PUT: Updating echo type with id: {Id}", id);
        
        var updated = await _mediator.Send(new UpdateEchoTypeCommand(id, request.Name, request.Description));
        if (updated == null)
        {
            _logger.LogWarning("Echo type with id {Id} not found", id);
            return NotFound(new { message = "Echo type not found" });
        }
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEchoType(string id)
    {
        _logger.LogInformation("DELETE: Deleting echo type with id: {Id}", id);
        
        var success = await _mediator.Send(new DeleteEchoTypeCommand(id));
        if (!success)
        {
            _logger.LogWarning("Echo type with id {Id} not found", id);
            return NotFound(new { message = "Echo type not found" });
        }
        return NoContent();
    }
}

public class CreateEchoTypeRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class UpdateEchoTypeRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
