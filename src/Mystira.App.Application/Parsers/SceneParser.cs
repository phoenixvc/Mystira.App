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
        if (sceneDict.TryGetValue("id", out var idObj) && idObj != null)
        {
            scene.Id = idObj.ToString() ?? string.Empty;
        }
        else
        {
            throw new ArgumentException("Required field 'id' is missing or null in scene data");
        }

        if (sceneDict.TryGetValue("title", out var titleObj) && titleObj != null)
        {
            scene.Title = titleObj.ToString() ?? string.Empty;
        }
        else
        {
            throw new ArgumentException("Required field 'title' is missing or null in scene data");
        }

        if (sceneDict.TryGetValue("description", out var descObj) && descObj != null)
        {
            scene.Description = descObj.ToString() ?? string.Empty;
        }
        else
        {
            throw new ArgumentException("Required field 'description' is missing or null in scene data");
        }

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
            if (Enum.TryParse<SceneType>(typeStr, true, out var sceneType))
            {
                scene.Type = sceneType;
            }
            else
            {
                throw new ArgumentException($"Invalid scene type: '{typeStr}'");
            }
        }

        // Parse difficulty (non-nullable, but has default value)
        if (sceneDict.TryGetValue("difficulty", out var difficultyObj) &&
            difficultyObj != null && int.TryParse(difficultyObj.ToString(), out var difficulty))
        {
            scene.Difficulty = difficulty;
        }
        else
        {
            scene.Difficulty = null;
        }

        // Parse media references (nullable)
        if (sceneDict.TryGetValue("media", out var mediaObj) && mediaObj is Dictionary<object, object> mediaDict)
        {
            var media = MediaReferencesParser.Parse(mediaDict);
            if (!string.IsNullOrWhiteSpace(media.Image) || !string.IsNullOrWhiteSpace(media.Audio) || !string.IsNullOrWhiteSpace(media.Video))
            {
                scene.Media = media;
            }
            else
            {
                scene.Media = null;
            }
        }

        // Parse branches (choices) - defaults to empty list if not found
        if (sceneDict.TryGetValue("branches", out var branchesObj) && branchesObj is IList<object> branchesList)
        {
            foreach (var branchObj in branchesList)
            {
                if (branchObj is IDictionary<object, object> branchDict)
                {
                    scene.Branches.Add(BranchParser.Parse(branchDict));
                }
            }
        }
        else if (sceneDict.TryGetValue("choices", out var choicesObj) && choicesObj is IList<object> choicesList)
        {
            foreach (var choiceObj in choicesList)
            {
                if (choiceObj is IDictionary<object, object> choiceDict)
                {
                    scene.Branches.Add(BranchParser.Parse(choiceDict));
                }
            }
        }

        // Parse Echo Reveal References - defaults to empty list if not found
        if (sceneDict.TryGetValue("echo_reveals", out var schemaEchoRevealsObj) && schemaEchoRevealsObj is IList<object> schemaEchoReveals)
        {
            foreach (var echoObj in schemaEchoReveals)
            {
                if (echoObj is IDictionary<object, object> echoDict)
                {
                    scene.EchoReveals.Add(EchoRevealParser.Parse(echoDict));
                }
            }
        }
        else if (sceneDict.TryGetValue("echoRevealReferences", out var legacyEchoRevealsObj) &&
                 legacyEchoRevealsObj is IList<object> legacyEchoRevealsList)
        {
            foreach (var echoObj in legacyEchoRevealsList)
            {
                if (echoObj is IDictionary<object, object> echoDict)
                {
                    scene.EchoReveals.Add(EchoRevealParser.Parse(echoDict));
                }
            }
        }

        return scene;
    }
}

