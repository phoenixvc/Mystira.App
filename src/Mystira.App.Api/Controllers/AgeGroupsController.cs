using Wolverine;
using Microsoft.AspNetCore.Mvc;
using Mystira.App.Application.CQRS.AgeGroups.Queries;
using Mystira.App.Api.Models;
using Mystira.App.Domain.Models;

namespace Mystira.App.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AgeGroupsController : ControllerBase
{
    private readonly IMessageBus _bus;
    private readonly ILogger<AgeGroupsController> _logger;

    public AgeGroupsController(IMessageBus bus, ILogger<AgeGroupsController> logger)
    {
        _bus = bus;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<AgeGroupDefinition>>> GetAllAgeGroups()
    {
        _logger.LogInformation("GET: Retrieving all age groups");
        var ageGroups = await _bus.InvokeAsync<List<AgeGroupDefinition>>(new GetAllAgeGroupsQuery());
        return Ok(ageGroups);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AgeGroupDefinition>> GetAgeGroupById(string id)
    {
        _logger.LogInformation("GET: Retrieving age group with id: {Id}", id);
        var ageGroup = await _bus.InvokeAsync<AgeGroupDefinition?>(new GetAgeGroupByIdQuery(id));
        if (ageGroup == null)
        {
            _logger.LogWarning("Age group with id {Id} not found", id);
            return NotFound(new { message = "Age group not found" });
        }
        return Ok(ageGroup);
    }

    [HttpPost("validate")]
    public async Task<ActionResult<ValidationResult>> ValidateAgeGroup([FromBody] ValidateAgeGroupRequest request)
    {
        _logger.LogInformation("POST: Validating age group: {Value}", request.Value);

        var isValid = await _bus.InvokeAsync<bool>(new ValidateAgeGroupQuery(request.Value));
        return Ok(new ValidationResult { IsValid = isValid });
    }
}

public class ValidateAgeGroupRequest
{
    public string Value { get; set; } = string.Empty;
}
