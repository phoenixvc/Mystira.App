using System.Text.Json.Serialization;

namespace Mystira.App.PWA.Models;

public class ScenariosResponse
{
    public List<Scenario> Scenarios { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public bool HasNextPage { get; set; }
}

public class Scenario
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    // Optional cover image media id for scenario cards (served from api/media/{id})
    public string? Image { get; set; }
    public List<Scene> Scenes { get; set; } = new();
    public string[] Tags { get; set; } = [];
    public string Difficulty { get; set; } = string.Empty;
    public string SessionLength { get; set; } = string.Empty;
    public string[] Archetypes { get; set; } = [];
    public int MinimumAge { get; set; } = 1;
    public string AgeGroup { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<string> CoreAxes { get; set; } = new();
    public List<ScenarioCharacter> Characters { get; set; } = new();

    // Backward compatibility properties
    public string Name => Title;
    public bool IsActive => true;
    public DateTime UpdatedAt => CreatedAt;
}

public class Scene
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? NextSceneId { get; set; }
    public SceneMedia? Media { get; set; }
    public List<SceneBranch> Branches { get; set; } = new();
    public int? Difficulty { get; set; }

    // Backward compatibility properties
    public string Content => Description;
    public SceneType SceneType => Type.ToLower() switch
    {
        "roll" => SceneType.Roll,
        "choice" => SceneType.Choice,
        "narrative" => SceneType.Narrative,
        "special" => SceneType.Special,
        _ => throw new ArgumentOutOfRangeException(
            nameof(Type),
            Type,
            "Expected one of 'roll', 'choice', 'narrative', or 'special' for scene type.")
    };

    public string? ImageUrl { get; set; }
    public string? VideoUrl { get; set; }
    public string? AudioUrl { get; set; }

    public List<Choice> Choices => Branches.Select((b, i) => new Choice
    {
        Id = i + 1,
        Text = b.Choice ?? "Continue",
        NextSceneId = b.NextSceneId ?? "",
        Order = i + 1
    }).Where(c => !string.IsNullOrEmpty(c.Text) && c.Text != "Continue").ToList();
    public bool IsStartingScene { get; set; } = false;
    public int Order { get; set; }

    // Roll scene specific properties
    public int? RollTarget => Difficulty > 0 ? Difficulty : null;
    public string? SuccessBranch { get; set; }
    public string? FailureBranch { get; set; }
    public string? SuccessSceneTitle { get; set; }
    public string? FailureSceneTitle { get; set; }
}

public class SceneMedia
{
    public string? Image { get; set; }
    public string? Audio { get; set; }
    public string? Video { get; set; }
}

public class SceneBranch
{
    public string? Choice { get; set; }
    public string? NextSceneId { get; set; }
}

public class Choice
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public string? NextSceneId { get; set; }
    public int Order { get; set; }
}

public class GameSession
{
    public string Id { get; set; } = string.Empty;
    public string ScenarioId { get; set; } = string.Empty;
    public string ScenarioName { get; set; } = string.Empty;
    public Scene? CurrentScene { get; set; }
    public List<Scene> CompletedScenes { get; set; } = new();
    public List<string> PlayerNames { get; set; } = new();
    [JsonPropertyName("startTime")]
    public DateTime StartedAt { get; set; }
    public bool IsCompleted { get; set; }
    public Scenario Scenario { get; set; } = new();
    public string CurrentSceneId { get; set; } = string.Empty;
    public int ChoiceCount { get; set; }
    public string Status { get; set; } = string.Empty;
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SceneType
{
    Narrative = 0,
    Choice = 1,
    Roll = 2,
    Special = 3
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MediaType
{
    Image = 0,
    Audio = 1,
    Video = 2
}

public class ScenarioCharacter
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Image { get; set; }
    public string? Audio { get; set; }
    public ScenarioCharacterMetadata Metadata { get; set; } = new();
}

public class ScenarioCharacterMetadata
{
    public List<string> Role { get; set; } = new();
    public List<string> Archetype { get; set; } = new();
    public string Species { get; set; } = string.Empty;
    public int Age { get; set; }
    public List<string> Traits { get; set; } = new();
    public string Backstory { get; set; } = string.Empty;
}
