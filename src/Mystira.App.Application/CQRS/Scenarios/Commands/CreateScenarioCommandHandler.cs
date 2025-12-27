using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Application.UseCases.Scenarios;
using Mystira.App.Application.Validation;
using Mystira.App.Application.Mappers;
using Mystira.Contracts.App.Requests.Scenarios;
using Mystira.App.Domain.Models;
using NJsonSchema;

namespace Mystira.App.Application.CQRS.Scenarios.Commands;

/// <summary>
/// Wolverine handler for CreateScenarioCommand.
/// Creates a new scenario - this is a write operation that modifies state.
/// </summary>
public static class CreateScenarioCommandHandler
{
    private static readonly JsonSchema ScenarioJsonSchema =
        JsonSchema.FromJsonAsync(ScenarioSchemaDefinitions.StorySchema).GetAwaiter().GetResult();

    private static readonly System.Text.Json.JsonSerializerOptions SchemaSerializerOptions = new()
    {
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    /// <summary>
    /// Handles the CreateScenarioCommand by creating a new scenario in the repository.
    /// Wolverine injects dependencies as method parameters.
    /// </summary>
    public static async Task<Scenario> Handle(
        CreateScenarioCommand command,
        IScenarioRepository repository,
        IUnitOfWork unitOfWork,
        ValidateScenarioUseCase validateScenarioUseCase,
        ILogger logger,
        CancellationToken ct)
    {
        var request = command.Request;

        // Validate against JSON schema
        ValidateAgainstSchema(request);

        // Create scenario entity
        var scenario = new Scenario
        {
            Id = Guid.NewGuid().ToString(),
            Title = request.Title,
            Description = request.Description,
            Tags = request.Tags ?? new List<string>(),
            Difficulty = ScenarioMapper.MapDifficultyLevel((int)request.Difficulty),
            SessionLength = ScenarioMapper.MapSessionLength((int)request.SessionLength),
            Archetypes = ScenarioMapper.ParseArchetypes(request.Archetypes),
            AgeGroup = request.AgeGroup,
            MinimumAge = request.MinimumAge,
            CoreAxes = ScenarioMapper.ParseCoreAxes(request.CoreAxes),
            Characters = request.Characters?.Select(ScenarioMapper.ToScenarioCharacter).ToList() ?? new List<ScenarioCharacter>(),
            Scenes = request.Scenes?.Select(ScenarioMapper.ToScene).ToList() ?? new List<Scene>(),
            CreatedAt = DateTime.UtcNow
        };

        // Validate scenario business rules
        await validateScenarioUseCase.ExecuteAsync(scenario);

        // Persist scenario
        await repository.AddAsync(scenario);

        try
        {
            await unitOfWork.SaveChangesAsync();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error saving scenario: {ScenarioId}", scenario.Id);
            throw;
        }

        logger.LogInformation("Created new scenario: {ScenarioId} - {Title}", scenario.Id, scenario.Title);
        return scenario;
    }

    private static void ValidateAgainstSchema(Contracts.App.Requests.Scenarios.CreateScenarioRequest request)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(request, SchemaSerializerOptions);
        var errors = ScenarioJsonSchema.Validate(json);

        if (errors.Count > 0)
        {
            var errorMessages = string.Join(", ", errors.Select(e => e.ToString()).ToList());
            throw new ArgumentException($"Scenario validation failed: {errorMessages}");
        }
    }
}
