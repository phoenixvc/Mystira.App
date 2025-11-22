using Microsoft.Extensions.Logging;
using Mystira.App.Application.Validation;
using Mystira.App.Contracts.Requests.Scenarios;
using Mystira.App.Domain.Models;
using Mystira.App.Infrastructure.Data.Repositories;
using Mystira.App.Infrastructure.Data.UnitOfWork;
using NJsonSchema;

namespace Mystira.App.Application.UseCases.Scenarios;

/// <summary>
/// Use case for creating a new scenario
/// </summary>
public class CreateScenarioUseCase
{
    private readonly IScenarioRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateScenarioUseCase> _logger;
    private readonly ValidateScenarioUseCase _validateScenarioUseCase;

    private static readonly JsonSchema ScenarioJsonSchema = JsonSchema.FromJsonAsync(ScenarioSchemaDefinitions.StorySchema).GetAwaiter().GetResult();

    private static readonly System.Text.Json.JsonSerializerOptions SchemaSerializerOptions = new()
    {
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    public CreateScenarioUseCase(
        IScenarioRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<CreateScenarioUseCase> logger,
        ValidateScenarioUseCase validateScenarioUseCase)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _validateScenarioUseCase = validateScenarioUseCase;
    }

    public async Task<Scenario> ExecuteAsync(CreateScenarioRequest request)
    {
        ValidateAgainstSchema(request);

        var scenario = new Scenario
        {
            Id = Guid.NewGuid().ToString(),
            Title = request.Title,
            Description = request.Description,
            Tags = request.Tags,
            Difficulty = request.Difficulty,
            SessionLength = request.SessionLength,
            Archetypes = request.Archetypes?.Select(a => Archetype.Parse(a)!).ToList() ?? new List<Archetype>(),
            AgeGroup = request.AgeGroup,
            MinimumAge = request.MinimumAge,
            CoreAxes = request.CoreAxes?.Select(a => CoreAxis.Parse(a)!).ToList() ?? new List<CoreAxis>(),
            Characters = request.Characters,
            Scenes = request.Scenes,
            CreatedAt = DateTime.UtcNow
        };

        await _validateScenarioUseCase.ExecuteAsync(scenario);

        await _repository.AddAsync(scenario);

        try
        {
            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error saving scenario: {ScenarioId}", scenario.Id);
            throw;
        }

        _logger.LogInformation("Created new scenario: {ScenarioId} - {Title}", scenario.Id, scenario.Title);
        return scenario;
    }

    private void ValidateAgainstSchema(CreateScenarioRequest request)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(request, SchemaSerializerOptions);
        var errors = ScenarioJsonSchema.Validate(json);

        if (errors.Count > 0)
        {
            var errorMessages = string.Join(", ", errors.Select(e => e.ToString()));
            throw new ArgumentException($"Scenario validation failed: {errorMessages}");
        }
    }
}

