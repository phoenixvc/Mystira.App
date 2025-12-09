using MediatR;
using Microsoft.AspNetCore.Mvc;
using Mystira.App.Application.CQRS.AgeGroups.Commands;
using Mystira.App.Application.CQRS.AgeGroups.Queries;
using Mystira.App.Domain.Models;

namespace Mystira.App.Admin.Api.Controllers;

[ApiController]
[Route("api/admin/[controller]")]
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

    [HttpPost]
    public async Task<ActionResult<AgeGroupDefinition>> CreateAgeGroup([FromBody] CreateAgeGroupRequest request)
    {
        _logger.LogInformation("POST: Creating age group with name: {Name}", request.Name);

        var created = await _mediator.Send(new CreateAgeGroupCommand(
            request.Name,
            request.Value,
            request.MinimumAge,
            request.MaximumAge,
            request.Description));
        return CreatedAtAction(nameof(GetAgeGroupById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<AgeGroupDefinition>> UpdateAgeGroup(string id, [FromBody] UpdateAgeGroupRequest request)
    {
        _logger.LogInformation("PUT: Updating age group with id: {Id}", id);

        var updated = await _mediator.Send(new UpdateAgeGroupCommand(
            id,
            request.Name,
            request.Value,
            request.MinimumAge,
            request.MaximumAge,
            request.Description));
        if (updated == null)
        {
            _logger.LogWarning("Age group with id {Id} not found", id);
            return NotFound(new { message = "Age group not found" });
        }
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAgeGroup(string id)
    {
        _logger.LogInformation("DELETE: Deleting age group with id: {Id}", id);

        var success = await _mediator.Send(new DeleteAgeGroupCommand(id));
        if (!success)
        {
            _logger.LogWarning("Age group with id {Id} not found", id);
            return NotFound(new { message = "Age group not found" });
        }
        return NoContent();
    }
}

public class CreateAgeGroupRequest
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public int MinimumAge { get; set; }
    public int MaximumAge { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class UpdateAgeGroupRequest
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public int MinimumAge { get; set; }
    public int MaximumAge { get; set; }
    public string Description { get; set; } = string.Empty;
}
