using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Application.UseCases.Scenarios;
using Mystira.App.Application.Validation;
using Mystira.Contracts.App.Requests.Scenarios;
using Mystira.App.Domain.Models;
using NJsonSchema;

namespace Mystira.App.Application.CQRS.Scenarios.Commands;

/// <summary>
/// Handler for CreateScenarioCommand - creates a new scenario
/// This is a write operation that modifies state
/// </summary>
public class CreateScenarioCommandHandler : ICommandHandler<CreateScenarioCommand, Scenario>
{
    private readonly IScenarioRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateScenarioCommandHandler> _logger;
    private readonly ValidateScenarioUseCase _validateScenarioUseCase;

    private static readonly JsonSchema ScenarioJsonSchema =
        JsonSchema.FromJsonAsync(ScenarioSchemaDefinitions.StorySchema).GetAwaiter().GetResult();

    private static readonly System.Text.Json.JsonSerializerOptions SchemaSerializerOptions = new()
    {
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    public CreateScenarioCommandHandler(
        IScenarioRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<CreateScenarioCommandHandler> logger,
        ValidateScenarioUseCase validateScenarioUseCase)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _validateScenarioUseCase = validateScenarioUseCase;
    }

    public async Task<Scenario> Handle(CreateScenarioCommand command, CancellationToken cancellationToken)
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
            Tags = request.Tags,
            Difficulty = (DifficultyLevel)(int)request.Difficulty,
            SessionLength = (SessionLength)(int)request.SessionLength,
            Archetypes = request.Archetypes?.Select(a => Archetype.Parse(a)!).ToList() ?? new List<Archetype>(),
            AgeGroup = request.AgeGroup,
            MinimumAge = request.MinimumAge,
            CoreAxes = request.CoreAxes?.Select(a => CoreAxis.Parse(a)!).ToList() ?? new List<CoreAxis>(),
            Characters = request.Characters?.Select(MapToScenarioCharacter).ToList() ?? new List<ScenarioCharacter>(),
            Scenes = request.Scenes?.Select(MapToScene).ToList() ?? new List<Scene>(),
            CreatedAt = DateTime.UtcNow
        };

        // Validate scenario business rules
        await _validateScenarioUseCase.ExecuteAsync(scenario);

        // Persist scenario
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

    private void ValidateAgainstSchema(Contracts.App.Requests.Scenarios.CreateScenarioRequest request)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(request, SchemaSerializerOptions);
        var errors = ScenarioJsonSchema.Validate(json);

        if (errors.Count > 0)
        {
            var errorMessages = string.Join(", ", errors.Select(e => e.ToString()).ToList());
            throw new ArgumentException($"Scenario validation failed: {errorMessages}");
        }
    }

    private static ScenarioCharacter MapToScenarioCharacter(CharacterRequest c)
    {
        return new ScenarioCharacter
        {
            Id = c.Id,
            Name = c.Name,
            Image = c.Image,
            Audio = c.Audio,
            Metadata = new ScenarioCharacterMetadata
            {
                Role = c.Metadata?.Role ?? new List<string>(),
                Archetype = c.Metadata?.Archetype?.Select(a => Archetype.Parse(a)!).ToList() ?? new List<Archetype>(),
                Species = c.Metadata?.Species ?? string.Empty,
                Age = c.Metadata?.Age ?? 0,
                Traits = c.Metadata?.Traits ?? new List<string>(),
                Backstory = c.Metadata?.Backstory ?? string.Empty
            }
        };
    }

    private static Scene MapToScene(SceneRequest s)
    {
        return new Scene
        {
            Id = s.Id,
            Title = s.Title,
            Type = Enum.TryParse<SceneType>(s.Type, true, out var sceneType) ? sceneType : SceneType.Narrative,
            Description = s.Description,
            NextSceneId = s.NextSceneId,
            Difficulty = s.Difficulty,
            ActiveCharacter = s.ActiveCharacter,
            Media = s.Media != null ? new MediaReferences
            {
                Image = s.Media.Image,
                Audio = s.Media.Audio,
                Video = s.Media.Video
            } : null,
            Branches = s.Branches?.Select(b => new Branch
            {
                Text = b.Text,
                NextSceneId = b.NextSceneId,
                CompassAxis = b.CompassAxis,
                CompassDirection = b.CompassDirection,
                CompassDelta = b.CompassDelta
            }).ToList() ?? new List<Branch>(),
            EchoReveals = s.EchoReveals?.Select(e => new EchoReveal
            {
                Condition = e.Condition,
                Message = e.Message,
                Tone = e.Tone
            }).ToList() ?? new List<EchoReveal>()
        };
    }
}
