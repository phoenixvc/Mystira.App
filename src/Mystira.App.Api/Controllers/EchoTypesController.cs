using MediatR;
using Microsoft.AspNetCore.Mvc;
using Mystira.App.Application.CQRS.EchoTypes.Queries;
using Mystira.App.Domain.Models;

namespace Mystira.App.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
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

    [HttpPost("validate")]
    public async Task<ActionResult<dynamic>> ValidateEchoType([FromBody] ValidateEchoTypeRequest request)
    {
        _logger.LogInformation("POST: Validating echo type: {Name}", request.Name);
        
        var isValid = await _mediator.Send(new ValidateEchoTypeQuery(request.Name));
        return Ok(new { isValid });
    }
}

public class ValidateEchoTypeRequest
{
    public string Name { get; set; } = string.Empty;
}
