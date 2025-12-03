using MediatR;
using Microsoft.AspNetCore.Mvc;
using Mystira.App.Application.CQRS.AgeGroups.Queries;
using Mystira.App.Domain.Models;

namespace Mystira.App.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AgeGroupsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AgeGroupsController> _logger;

    public AgeGroupsController(IMediator mediator, ILogger<AgeGroupsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<AgeGroupDefinition>>> GetAllAgeGroups()
    {
        _logger.LogInformation("GET: Retrieving all age groups");
        var ageGroups = await _mediator.Send(new GetAllAgeGroupsQuery());
        return Ok(ageGroups);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AgeGroupDefinition>> GetAgeGroupById(string id)
    {
        _logger.LogInformation("GET: Retrieving age group with id: {Id}", id);
        var ageGroup = await _mediator.Send(new GetAgeGroupByIdQuery(id));
        if (ageGroup == null)
        {
            _logger.LogWarning("Age group with id {Id} not found", id);
            return NotFound(new { message = "Age group not found" });
        }
        return Ok(ageGroup);
    }

    [HttpPost("validate")]
    public async Task<ActionResult<dynamic>> ValidateAgeGroup([FromBody] ValidateAgeGroupRequest request)
    {
        _logger.LogInformation("POST: Validating age group: {Value}", request.Value);
        
        var isValid = await _mediator.Send(new ValidateAgeGroupQuery(request.Value));
        return Ok(new { isValid });
    }
}

public class ValidateAgeGroupRequest
{
    public string Value { get; set; } = string.Empty;
}
