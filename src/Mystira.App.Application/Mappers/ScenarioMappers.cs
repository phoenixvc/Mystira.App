using Mystira.App.Domain.Models;
using Mystira.Contracts.App.Requests.Scenarios;

namespace Mystira.App.Application.Mappers;

/// <summary>
/// Shared mapping utilities for scenario-related conversions.
/// Extracts common mapping logic to avoid duplication across use cases and handlers.
/// </summary>
public static class ScenarioMappers
{
    /// <summary>
    /// Safely converts a request difficulty enum to domain DifficultyLevel.
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
    /// Safely converts a request session length enum to domain SessionLength.
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
    /// Maps a CharacterRequest to a ScenarioCharacter domain model.
    /// Safely handles null Archetype.Parse results by filtering them out.
    /// </summary>
    /// <param name="characterRequest">The character request to map</param>
    /// <returns>A ScenarioCharacter domain model</returns>
    public static ScenarioCharacter MapToScenarioCharacter(CharacterRequest characterRequest)
    {
        return new ScenarioCharacter
        {
            Id = characterRequest.Id,
            Name = characterRequest.Name,
            Image = characterRequest.Image,
            Audio = characterRequest.Audio,
            Metadata = new ScenarioCharacterMetadata
            {
                Role = characterRequest.Metadata?.Role ?? new List<string>(),
                Archetype = characterRequest.Metadata?.Archetype?
                    .Select(a => Archetype.Parse(a))
                    .Where(a => a != null)
                    .Select(a => a!)
                    .ToList() ?? new List<Archetype>(),
                Species = characterRequest.Metadata?.Species ?? string.Empty,
                Age = characterRequest.Metadata?.Age ?? 0,
                Traits = characterRequest.Metadata?.Traits ?? new List<string>(),
                Backstory = characterRequest.Metadata?.Backstory ?? string.Empty
            }
        };
    }

    /// <summary>
    /// Maps a SceneRequest to a Scene domain model.
    /// </summary>
    /// <param name="sceneRequest">The scene request to map</param>
    /// <returns>A Scene domain model</returns>
    public static Scene MapToScene(SceneRequest sceneRequest)
    {
        return new Scene
        {
            Id = sceneRequest.Id,
            Title = sceneRequest.Title,
            Type = Enum.TryParse<SceneType>(sceneRequest.Type, true, out var sceneType) 
                ? sceneType 
                : SceneType.Narrative,
            Description = sceneRequest.Description,
            NextSceneId = sceneRequest.NextSceneId,
            Difficulty = sceneRequest.Difficulty,
            ActiveCharacter = sceneRequest.ActiveCharacter,
            Media = sceneRequest.Media != null ? new MediaReferences
            {
                Image = sceneRequest.Media.Image,
                Audio = sceneRequest.Media.Audio,
                Video = sceneRequest.Media.Video
            } : null,
            Branches = sceneRequest.Branches?.Select(b => new Branch
            {
                Text = b.Text,
                NextSceneId = b.NextSceneId,
                CompassAxis = b.CompassAxis,
                CompassDirection = b.CompassDirection,
                CompassDelta = b.CompassDelta
            }).ToList() ?? new List<Branch>(),
            EchoReveals = sceneRequest.EchoReveals?.Select(e => new EchoReveal
            {
                Condition = e.Condition,
                Message = e.Message,
                Tone = e.Tone
            }).ToList() ?? new List<EchoReveal>()
        };
    }

    /// <summary>
    /// Safely parses a list of archetype strings, filtering out any null results.
    /// </summary>
    /// <param name="archetypes">List of archetype strings to parse</param>
    /// <returns>List of parsed Archetype objects, excluding nulls</returns>
    public static List<Archetype> ParseArchetypes(IEnumerable<string>? archetypes)
    {
        if (archetypes == null)
        {
            return new List<Archetype>();
        }

        return archetypes
            .Select(a => Archetype.Parse(a))
            .Where(a => a != null)
            .Select(a => a!)
            .ToList();
    }

    /// <summary>
    /// Safely parses a list of core axis strings, filtering out any null results.
    /// </summary>
    /// <param name="coreAxes">List of core axis strings to parse</param>
    /// <returns>List of parsed CoreAxis objects, excluding nulls</returns>
    public static List<CoreAxis> ParseCoreAxes(IEnumerable<string>? coreAxes)
    {
        if (coreAxes == null)
        {
            return new List<CoreAxis>();
        }

        return coreAxes
            .Select(a => CoreAxis.Parse(a))
            .Where(a => a != null)
            .Select(a => a!)
            .ToList();
    }
}
