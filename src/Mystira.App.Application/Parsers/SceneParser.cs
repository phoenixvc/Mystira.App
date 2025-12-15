using Mystira.App.Domain.Models;

namespace Mystira.App.Application.Parsers;

/// <summary>
/// Parser for converting scene dictionary data to Scene domain object
/// </summary>
public static class SceneParser
{
    public static Scene Parse(IDictionary<object, object> sceneDict)
    {
        var scene = new Scene();

        // Parse required string properties (non-nullable)
        if (!sceneDict.TryGetValue("id", out var idObj) || idObj == null)
        {
            throw new ArgumentException("Required field 'id' is missing or null in scene data");
        }
        scene.Id = idObj.ToString() ?? string.Empty;

        if (!sceneDict.TryGetValue("title", out var titleObj) || titleObj == null)
        {
            throw new ArgumentException("Required field 'title' is missing or null in scene data");
        }
        scene.Title = titleObj.ToString() ?? string.Empty;

        if (!sceneDict.TryGetValue("description", out var descObj) || descObj == null)
        {
            throw new ArgumentException("Required field 'description' is missing or null in scene data");
        }
        scene.Description = descObj.ToString() ?? string.Empty;

        // Parse next scene ID (nullable)
        if (sceneDict.TryGetValue("nextSceneId", out var nextSceneObj) ||
            sceneDict.TryGetValue("next_scene_id", out nextSceneObj) ||
            sceneDict.TryGetValue("next_scene", out nextSceneObj))
        {
            var nextSceneValue = nextSceneObj?.ToString();
            scene.NextSceneId = string.IsNullOrWhiteSpace(nextSceneValue) ? null : nextSceneValue;
        }

        // Parse SceneType enum (non-nullable)
        if (sceneDict.TryGetValue("type", out var typeObj) && typeObj != null)
        {
            var typeStr = typeObj.ToString();
            if (!Enum.TryParse<SceneType>(typeStr, true, out var sceneType))
            {
                throw new ArgumentException($"Invalid scene type: '{typeStr}'");
            }
            scene.Type = sceneType;
        }

        // Parse difficulty (nullable)
        scene.Difficulty = sceneDict.TryGetValue("difficulty", out var difficultyObj) &&
                           difficultyObj != null &&
                           int.TryParse(difficultyObj.ToString(), out var difficulty)
            ? difficulty
            : null;

        // Parse media references (nullable)
        if (sceneDict.TryGetValue("media", out var mediaObj) && mediaObj is Dictionary<object, object> mediaDict)
        {
            var media = MediaReferencesParser.Parse(mediaDict);
            scene.Media = !string.IsNullOrWhiteSpace(media.Image) ||
                          !string.IsNullOrWhiteSpace(media.Audio) ||
                          !string.IsNullOrWhiteSpace(media.Video)
                ? media
                : null;
        }

        // Parse active character (nullable)
        if (sceneDict.TryGetValue("active_character", out var activeCharObj) ||
            sceneDict.TryGetValue("activeCharacter", out activeCharObj))
        {
            var ac = activeCharObj?.ToString();
            scene.ActiveCharacter = string.IsNullOrWhiteSpace(ac) ? null : ac;
        }

        // Parse branches (choices) - defaults to empty list if not found
        var branchesList = sceneDict.TryGetValue("branches", out var branchesObj) && branchesObj is IList<object> bl
            ? bl
            : sceneDict.TryGetValue("choices", out var choicesObj) && choicesObj is IList<object> cl
                ? cl
                : null;

        if (branchesList != null)
        {
            foreach (var branchObj in branchesList.OfType<IDictionary<object, object>>())
            {
                scene.Branches.Add(BranchParser.Parse(branchObj));
            }
        }

        // Parse Echo Reveal References - defaults to empty list if not found
        var echoRevealsList = sceneDict.TryGetValue("echo_reveals", out var schemaEchoRevealsObj) && schemaEchoRevealsObj is IList<object> sel
            ? sel
            : sceneDict.TryGetValue("echoRevealReferences", out var legacyEchoRevealsObj) && legacyEchoRevealsObj is IList<object> lel
                ? lel
                : null;

        if (echoRevealsList != null)
        {
            foreach (var echoObj in echoRevealsList.OfType<IDictionary<object, object>>())
            {
                scene.EchoReveals.Add(EchoRevealParser.Parse(echoObj));
            }
        }

        return scene;
    }
}

