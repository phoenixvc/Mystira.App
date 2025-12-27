using Mystira.App.Domain.Models;
using Mystira.Contracts.App.Requests.Scenarios;
using Riok.Mapperly.Abstractions;

namespace Mystira.App.Application.Mappers;

/// <summary>
/// Mapperly-based mapper for scenario-related types.
/// Uses compile-time source generation for optimal performance.
/// </summary>
[Mapper]
public static partial class ScenarioMapper
{
    /// <summary>
    /// Maps a CharacterRequest to a ScenarioCharacter domain model.
    /// </summary>
    public static partial ScenarioCharacter ToScenarioCharacter(CharacterRequest request);

    /// <summary>
    /// Maps CharacterMetadataRequest to ScenarioCharacterMetadata.
    /// </summary>
    [MapProperty(nameof(CharacterMetadataRequest.Archetype), nameof(ScenarioCharacterMetadata.Archetype), Use = nameof(MapArchetypes))]
    private static partial ScenarioCharacterMetadata ToScenarioCharacterMetadata(CharacterMetadataRequest request);

    /// <summary>
    /// Maps a SceneRequest to a Scene domain model.
    /// </summary>
    [MapProperty(nameof(SceneRequest.Type), nameof(Scene.Type), Use = nameof(ParseSceneType))]
    [MapProperty(nameof(SceneRequest.Difficulty), nameof(Scene.Difficulty), Use = nameof(ParseDifficulty))]
    [MapProperty(nameof(SceneRequest.Media), nameof(Scene.Media), Use = nameof(MapMedia))]
    [MapProperty(nameof(SceneRequest.Branches), nameof(Scene.Branches), Use = nameof(MapBranches))]
    [MapProperty(nameof(SceneRequest.EchoReveals), nameof(Scene.EchoReveals), Use = nameof(MapEchoReveals))]
    public static partial Scene ToScene(SceneRequest request);

    /// <summary>
    /// Maps a BranchRequest to a Branch domain model.
    /// </summary>
    public static partial Branch ToBranch(BranchRequest request);

    /// <summary>
    /// Safely converts a request difficulty int to domain DifficultyLevel.
    /// Validates that the input value corresponds to a defined enum value.
    /// </summary>
    /// <param name="requestDifficulty">The difficulty level from the request</param>
    /// <returns>The corresponding domain DifficultyLevel</returns>
    /// <exception cref="ArgumentException">Thrown when the input value is not a valid DifficultyLevel</exception>
    public static DifficultyLevel MapDifficultyLevel(int requestDifficulty)
    {
        if (!Enum.IsDefined(typeof(DifficultyLevel), requestDifficulty))
        {
            var validValues = string.Join(", ", Enum.GetValues<DifficultyLevel>()
                .Select(e => $"{e} ({(int)e})"));
            throw new ArgumentException(
                $"Invalid difficulty level: {requestDifficulty}. Valid values are: {validValues}",
                nameof(requestDifficulty));
        }

        return (DifficultyLevel)requestDifficulty;
    }

    /// <summary>
    /// Safely converts a request session length int to domain SessionLength.
    /// Validates that the input value corresponds to a defined enum value.
    /// </summary>
    /// <param name="requestSessionLength">The session length from the request</param>
    /// <returns>The corresponding domain SessionLength</returns>
    /// <exception cref="ArgumentException">Thrown when the input value is not a valid SessionLength</exception>
    public static SessionLength MapSessionLength(int requestSessionLength)
    {
        if (!Enum.IsDefined(typeof(SessionLength), requestSessionLength))
        {
            var validValues = string.Join(", ", Enum.GetValues<SessionLength>()
                .Select(e => $"{e} ({(int)e})"));
            throw new ArgumentException(
                $"Invalid session length: {requestSessionLength}. Valid values are: {validValues}",
                nameof(requestSessionLength));
        }

        return (SessionLength)requestSessionLength;
    }

    /// <summary>
    /// Safely parses a list of archetype strings, filtering out any null results.
    /// </summary>
    public static List<Archetype> ParseArchetypes(IEnumerable<string>? archetypes)
    {
        if (archetypes == null)
        {
            return new List<Archetype>();
        }

        return archetypes
            .Select(Archetype.Parse)
            .OfType<Archetype>()
            .ToList();
    }

    /// <summary>
    /// Safely parses a list of core axis strings, filtering out any null results.
    /// </summary>
    public static List<CoreAxis> ParseCoreAxes(IEnumerable<string>? coreAxes)
    {
        if (coreAxes == null)
        {
            return new List<CoreAxis>();
        }

        return coreAxes
            .Select(CoreAxis.Parse)
            .OfType<CoreAxis>()
            .ToList();
    }

    // Custom mapping methods for complex scenarios

    private static List<Archetype> MapArchetypes(List<string>? archetypes)
        => ParseArchetypes(archetypes);

    private static SceneType ParseSceneType(string? type)
        => Enum.TryParse<SceneType>(type, true, out var sceneType)
            ? sceneType
            : SceneType.Narrative;

    private static int? ParseDifficulty(string? difficulty)
        => string.IsNullOrEmpty(difficulty) ? null : int.TryParse(difficulty, out var diff) ? diff : null;

    private static MediaReferences? MapMedia(MediaReferencesRequest? media)
        => media != null ? new MediaReferences
        {
            Image = media.Image,
            Audio = media.Audio,
            Video = media.Video
        } : null;

    private static List<Branch> MapBranches(List<BranchRequest>? branches)
        => branches?.Select(b => new Branch
        {
            Choice = b.Text ?? string.Empty,
            NextSceneId = b.NextSceneId ?? string.Empty,
            CompassChange = (b.CompassAxis != null || b.CompassDelta.HasValue)
                ? new CompassChange
                {
                    Axis = b.CompassAxis ?? string.Empty,
                    Delta = b.CompassDelta ?? 0.0
                }
                : null
        }).ToList() ?? new List<Branch>();

    private static List<EchoReveal> MapEchoReveals(List<EchoRevealRequest>? echoReveals)
        => echoReveals?.Select(e => new EchoReveal
        {
            EchoType = e.Tone ?? "honesty",
            TriggerSceneId = e.Condition ?? string.Empty,
            RevealMechanic = e.Message
        }).ToList() ?? new List<EchoReveal>();
}
