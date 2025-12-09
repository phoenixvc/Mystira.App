namespace Mystira.App.Contracts.Responses.Scenarios;

/// <summary>
/// Represents a scenario reference validation result
/// </summary>
public class ScenarioReferenceValidation
{
    public string ScenarioId { get; set; } = string.Empty;
    public string ScenarioTitle { get; set; } = string.Empty;
    public List<MediaReference> MediaReferences { get; set; } = new();
    public List<CharacterReference> CharacterReferences { get; set; } = new();
    public List<MissingReference> MissingReferences { get; set; } = new();
    public bool HasMissingReferences => MissingReferences.Any();
    public int TotalReferences => MediaReferences.Count + CharacterReferences.Count;
    public int MissingReferencesCount => MissingReferences.Count;
}

/// <summary>
/// Represents a media reference in a scenario
/// </summary>
public class MediaReference
{
    public string SceneId { get; set; } = string.Empty;
    public string SceneTitle { get; set; } = string.Empty;
    public string MediaId { get; set; } = string.Empty;
    public string MediaType { get; set; } = string.Empty; // image, audio, video
    public bool HasMetadata { get; set; }
    public bool MediaExists { get; set; }
}

/// <summary>
/// Represents a character reference in a scenario
/// </summary>
public class CharacterReference
{
    public string SceneId { get; set; } = string.Empty;
    public string SceneTitle { get; set; } = string.Empty;
    public string CharacterId { get; set; } = string.Empty;
    public string CharacterName { get; set; } = string.Empty;
    public bool HasMetadata { get; set; }
    public bool CharacterExists { get; set; }
}

/// <summary>
/// Represents a missing reference (media or character)
/// </summary>
public class MissingReference
{
    public string ReferenceId { get; set; } = string.Empty;
    public string ReferenceType { get; set; } = string.Empty; // media, character
    public string SceneId { get; set; } = string.Empty;
    public string SceneTitle { get; set; } = string.Empty;
    public string IssueType { get; set; } = string.Empty; // missing_file, missing_metadata, invalid_reference
    public string Description { get; set; } = string.Empty;
}

