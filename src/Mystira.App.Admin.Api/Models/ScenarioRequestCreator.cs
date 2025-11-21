using System.Collections;
using System.Data;
using System.Linq;
using Mystira.App.Domain.Models;

namespace Mystira.App.Admin.Api.Models;

public static class ScenarioRequestCreator
{
    public static CreateScenarioRequest Create(Dictionary<object, object> scenarioData)
    {
        if (!scenarioData.TryGetValue("title", out var title))
        {
            throw new DataException("Scenario does not contain a title.");
        }

        if (!scenarioData.TryGetValue("description", out var description))
        {
            throw new DataException("Scenario does not contain a description.");
        }

        if (!scenarioData.TryGetValue("tags", out var tags))
        {
            throw new DataException("Scenario does not contain tags.");
        }

        if (!scenarioData.TryGetValue("difficulty", out var d))
        {
            throw new DataException("Scenario does not contain a difficulty.");
        }

        if (!Enum.TryParse<DifficultyLevel>((string)d, true, out var difficulty))
        {
            throw new DataException("Scenario does not contain a valid difficulty level.");
        }

        if (!scenarioData.TryGetValue("session_length", out var s))
        {
            throw new DataException("Scenario does not contain session_length.");
        }

        if (!Enum.TryParse<SessionLength>((string)s, true, out var sessionLength))
        {
            throw new DataException("Scenario does not contain a valid session_length.");
        }

        if (!scenarioData.TryGetValue("archetypes", out var archetypes))
        {
            throw new DataException("Scenario does not contain archetypes.");
        }

        if (!scenarioData.TryGetValue("age_group", out var ageGroup))
        {
            throw new DataException("Scenario does not contain age_group.");
        }

        if (!scenarioData.TryGetValue("minimum_age", out var minimumAge))
        {
            throw new DataException("Scenario does not contain minimum_age.");
        }

        var coreAxesRaw = scenarioData.GetValueOrDefault("core_axes")
                          ?? scenarioData.GetValueOrDefault("compass_axes", new List<object>());
        if (!scenarioData.TryGetValue("characters", out var charactersObj) || charactersObj is not IList<object>)
        {
            throw new DataException("Scenario does not contain characters.");
        }

        var scenes = (List<object>)scenarioData.GetValueOrDefault("scenes", new List<object>());
        if (scenes.Count == 0)
        {
            throw new Exception("Scenario does not contain any scenes.");
        }

        var coreAxesList = ToStringList(coreAxesRaw);

        // Convert to CreateScenarioRequest format
        var createRequest = new CreateScenarioRequest
        {
            Title = (string)title,
            Description = (string)description,
            Tags = ((List<object>)tags).Select(o => (string)o).ToList(),
            Difficulty = difficulty,
            SessionLength = sessionLength,
            Archetypes = ((List<object>)archetypes).Select(o => (string)o).ToList(),
            AgeGroup = ageGroup?.ToString() ?? string.Empty,
            MinimumAge = Convert.ToInt32(minimumAge),
            CoreAxes = coreAxesList,
            Characters = ((IEnumerable<object>)charactersObj).Select(o => ParseCharacter((Dictionary<object, object>)o)).ToList(),
            Scenes = scenes.Select(o => ParseSceneFromDictionary((Dictionary<object, object>)o)).ToList()
        };

        createRequest.CompassAxes = createRequest.CoreAxes;

        return createRequest;
    }

    public static Scene ParseSceneFromDictionary(IDictionary<object, object> sceneDict)
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
            var media = ParseMediaReferences(mediaDict);
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
                    scene.Branches.Add(ParseBranch(branchDict));
                }
            }
        }
        else if (sceneDict.TryGetValue("choices", out var choicesObj) && choicesObj is IList<object> choicesList)
        {
            foreach (var choiceObj in choicesList)
            {
                if (choiceObj is IDictionary<object, object> choiceDict)
                {
                    scene.Branches.Add(ParseBranch(choiceDict));
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
                    scene.EchoReveals.Add(ParseEchoRevealReference(echoDict));
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
                    scene.EchoReveals.Add(ParseEchoRevealReference(echoDict));
                }
            }
        }

        return scene;
    }

    private static MediaReferences ParseMediaReferences(IDictionary<object, object> mediaDict)
    {
        var media = new MediaReferences();

        // Check for Image field with various naming conventions
        if (mediaDict.TryGetValue("image", out var imageObj) ||
            mediaDict.TryGetValue("Image", out imageObj))
        {
            media.Image = imageObj?.ToString();
        }

        // Check for Audio field with various naming conventions
        if (mediaDict.TryGetValue("audio", out var audioObj) ||
            mediaDict.TryGetValue("Audio", out audioObj))
        {
            media.Audio = audioObj?.ToString();
        }

        // Check for Video field with various naming conventions
        if (mediaDict.TryGetValue("video", out var videoObj) ||
            mediaDict.TryGetValue("Video", out videoObj))
        {
            media.Video = videoObj?.ToString();
        }

        return media;
    }

    private static ScenarioCharacter ParseCharacter(IDictionary<object, object> characterDict)
    {
        if (!characterDict.TryGetValue("id", out var idObj) || idObj == null)
        {
            throw new ArgumentException("Required field 'id' is missing or null in character data");
        }

        if (!characterDict.TryGetValue("name", out var nameObj) || nameObj == null)
        {
            throw new ArgumentException("Required field 'name' is missing or null in character data");
        }

        var character = new ScenarioCharacter
        {
            Id = idObj.ToString() ?? string.Empty,
            Name = nameObj.ToString() ?? string.Empty
        };

        if (characterDict.TryGetValue("image", out var imageObj))
        {
            character.Image = imageObj?.ToString();
        }

        if (characterDict.TryGetValue("audio", out var audioObj))
        {
            character.Audio = audioObj?.ToString();
        }

        if (!characterDict.TryGetValue("metadata", out var metadataObj) || metadataObj is not IDictionary<object, object> metadataDict)
        {
            throw new ArgumentException("Required field 'metadata' is missing or invalid in character data");
        }

        character.Metadata = ParseCharacterMetadata(metadataDict);

        return character;
    }

    private static ScenarioCharacterMetadata ParseCharacterMetadata(IDictionary<object, object> metadataDict)
    {
        metadataDict.TryGetValue("role", out var roleObj);
        metadataDict.TryGetValue("archetype", out var archetypeObj);
        metadataDict.TryGetValue("traits", out var traitsObj);

        var metadata = new ScenarioCharacterMetadata
        {
            Role = ToStringList(roleObj),
            Archetype = ToStringList(archetypeObj).Select(a => Archetype.Parse(a)!).ToList(),
            Traits = ToStringList(traitsObj)
        };

        if (!metadataDict.TryGetValue("species", out var speciesObj) || speciesObj == null)
        {
            throw new ArgumentException("Required field 'species' is missing or null in character metadata");
        }

        metadata.Species = speciesObj.ToString() ?? string.Empty;

        if (!metadataDict.TryGetValue("age", out var ageObj) || ageObj == null || !int.TryParse(ageObj.ToString(), out var age))
        {
            throw new ArgumentException("Required field 'age' is missing or invalid in character metadata");
        }

        metadata.Age = age;

        if (!metadataDict.TryGetValue("backstory", out var backstoryObj) || backstoryObj == null)
        {
            throw new ArgumentException("Required field 'backstory' is missing or null in character metadata");
        }

        metadata.Backstory = backstoryObj.ToString() ?? string.Empty;

        return metadata;
    }

    private static List<string> ToStringList(object? value)
    {
        if (value is string single && !string.IsNullOrWhiteSpace(single))
        {
            return new List<string> { single };
        }

        if (value is IEnumerable enumerable)
        {
            var results = new List<string>();
            foreach (var item in enumerable)
            {
                var str = item?.ToString();
                if (!string.IsNullOrWhiteSpace(str))
                {
                    results.Add(str!);
                }
            }
            return results;
        }

        return new List<string>();
    }

    private static List<T> ToEnumList<T>(object? value) where T : StringEnum<T>
    {
        var results = new List<T>();
        foreach (var entry in ToStringList(value))
        {
            var parsed = StringEnum<T>.Parse(entry);
            if (parsed != null)
            {
                results.Add(parsed);
            }
        }

        return results;
    }

    private static Branch ParseBranch(IDictionary<object, object> branchDict)
    {
        var branch = new Branch();

        // Parse required Choice field (replaces Text field)
        if (branchDict.TryGetValue("choice", out var choiceObj) ||
            branchDict.TryGetValue("text", out choiceObj) ||
            branchDict.TryGetValue("option", out choiceObj))
        {
            if (choiceObj != null)
            {
                branch.Choice = choiceObj.ToString() ?? string.Empty;
            }
            else
            {
                throw new ArgumentException("Required field 'choice'/'text' is missing or null in branch data");
            }
        }
        else
        {
            throw new ArgumentException("Required field 'choice'/'text' is missing in branch data");
        }

        // Parse required NextSceneId field
        if (branchDict.TryGetValue("nextSceneId", out var nextSceneObj) ||
            branchDict.TryGetValue("next_scene_id", out nextSceneObj) ||
            branchDict.TryGetValue("next_scene", out nextSceneObj))
        {
            if (nextSceneObj != null)
            {
                var nextScene = nextSceneObj.ToString();
                branch.NextSceneId = string.IsNullOrWhiteSpace(nextScene) ? string.Empty : nextScene;
            }
            else
            {
                throw new ArgumentException("Required field 'nextSceneId'/'next_scene' is missing or null in branch data");
            }
        }
        else
        {
            throw new ArgumentException("Required field 'nextSceneId'/'next_scene' is missing in branch data");
        }

        // Parse EchoLog if available
        if (branchDict.TryGetValue("echoLog", out var echoLogObj) ||
            branchDict.TryGetValue("echo_log", out echoLogObj))
        {
            if (echoLogObj is IDictionary<object, object> echoLogDict)
            {
                branch.EchoLog = ParseEchoLog(echoLogDict);
            }
        }

        // Parse CompassChange if available
        if (branchDict.TryGetValue("compassChange", out var compassChangeObj) ||
            branchDict.TryGetValue("compass_change", out compassChangeObj) ||
            branchDict.TryGetValue("compass_impact", out compassChangeObj))
        {
            if (compassChangeObj is IDictionary<object, object> compassChangeDict)
            {
                branch.CompassChange = ParseCompassChange(compassChangeDict);
            }
        }

        return branch;
    }

    private static EchoLog ParseEchoLog(IDictionary<object, object> echoLogDict)
    {
        var echoLog = new EchoLog
        {
            Timestamp = DateTime.UtcNow // Default to current UTC time
        };

        // Parse EchoType (required)
        if (echoLogDict.TryGetValue("echoType", out var echoTypeObj) ||
            echoLogDict.TryGetValue("echo_type", out echoTypeObj) ||
            echoLogDict.TryGetValue("type", out echoTypeObj))
        {
            if (echoTypeObj != null)
            {
                var parsed = EchoType.Parse(echoTypeObj.ToString());
                if (parsed == null)
                    throw new ArgumentException($"Invalid EchoType: {echoTypeObj}");
                echoLog.EchoType = parsed;
            }
            else
            {
                throw new ArgumentException("Required field 'echoType'/'type' is missing or null in echo log data");
            }
        }
        else
        {
            throw new ArgumentException("Required field 'echoType'/'type' is missing in echo log data");
        }

        // Parse Description (required)
        if (echoLogDict.TryGetValue("description", out var descObj) ||
            echoLogDict.TryGetValue("message", out descObj) ||
            echoLogDict.TryGetValue("text", out descObj))
        {
            if (descObj != null)
            {
                echoLog.Description = descObj.ToString() ?? string.Empty;
            }
            else
            {
                throw new ArgumentException("Required field 'description'/'message' is missing or null in echo log data");
            }
        }
        else
        {
            throw new ArgumentException("Required field 'description'/'message' is missing in echo log data");
        }

        // Parse Strength (with validation)
        if (echoLogDict.TryGetValue("strength", out var strengthObj) ||
            echoLogDict.TryGetValue("power", out strengthObj) ||
            echoLogDict.TryGetValue("intensity", out strengthObj))
        {
            if (strengthObj != null &&
                double.TryParse(strengthObj.ToString(), out double strength))
            {
                // Validate strength is between 0.1 and 1.0
                echoLog.Strength = Math.Clamp(strength, 0.1, 1.0);
            }
            else
            {
                // Default to mid-range if not specified or invalid
                echoLog.Strength = 0.5;
            }
        }
        else
        {
            // Default to mid-range if not specified
            echoLog.Strength = 0.5;
        }

        // Parse Timestamp if provided (otherwise use default UTC now)
        if (echoLogDict.TryGetValue("timestamp", out var timestampObj) ||
            echoLogDict.TryGetValue("time", out timestampObj) ||
            echoLogDict.TryGetValue("date", out timestampObj))
        {
            if (timestampObj != null)
            {
                var timestampStr = timestampObj.ToString();
                if (!string.IsNullOrEmpty(timestampStr) &&
                    DateTime.TryParse(timestampStr, out DateTime timestamp))
                {
                    echoLog.Timestamp = timestamp;
                }
            }
        }

        return echoLog;
    }

    private static CompassChange ParseCompassChange(IDictionary<object, object> compassChangeDict)
    {
        var compassChange = new CompassChange();

        // Parse Axis (required)
        if (compassChangeDict.TryGetValue("axis", out var axisObj) ||
            compassChangeDict.TryGetValue("compass_axis", out axisObj) ||
            compassChangeDict.TryGetValue("value", out axisObj))
        {
            if (axisObj != null)
            {
                compassChange.Axis = axisObj.ToString() ?? string.Empty;
            }
            else
            {
                throw new ArgumentException("Required field 'axis' is missing or null in compass change data");
            }
        }
        else
        {
            throw new ArgumentException("Required field 'axis' is missing in compass change data");
        }

        // Parse Delta (required) with validation
        if (compassChangeDict.TryGetValue("delta", out var deltaObj) ||
            compassChangeDict.TryGetValue("change", out deltaObj) ||
            compassChangeDict.TryGetValue("impact", out deltaObj) ||
            compassChangeDict.TryGetValue("value", out deltaObj))
        {
            if (deltaObj != null &&
                double.TryParse(deltaObj.ToString(), out double delta))
            {
                // Validate delta is between -1.0 and 1.0
                compassChange.Delta = Math.Clamp(delta, -1.0, 1.0);
            }
            else
            {
                throw new ArgumentException("Required field 'delta'/'change'/'impact' is invalid or null in compass change data");
            }
        }
        else
        {
            throw new ArgumentException("Required field 'delta'/'change'/'impact' is missing in compass change data");
        }

        if (compassChangeDict.TryGetValue("developmental_link", out var devLinkObj) ||
            compassChangeDict.TryGetValue("developmentalLink", out devLinkObj))
        {
            compassChange.DevelopmentalLink = devLinkObj?.ToString();
        }

        return compassChange;
    }

    private static EchoReveal ParseEchoRevealReference(IDictionary<object, object> revealDict)
    {
        var reveal = new EchoReveal();

        // Parse EchoType (required)
        if (revealDict.TryGetValue("echoType", out var echoTypeObj) ||
            revealDict.TryGetValue("echo_type", out echoTypeObj) ||
            revealDict.TryGetValue("type", out echoTypeObj))
        {
            if (echoTypeObj != null)
            {
<<<<<<< HEAD
                var echoTypeStr = echoTypeObj.ToString();
                var parsedEchoType = EchoType.Parse(echoTypeStr);
                if (parsedEchoType == null)
                {
                    throw new ArgumentException($"Invalid echo type: '{echoTypeStr}'");
                }
                reveal.EchoType = parsedEchoType;
=======
                var parsed = EchoType.Parse(echoTypeObj.ToString());
                if (parsed == null)
                    throw new ArgumentException($"Invalid EchoType: {echoTypeObj}");
                reveal.EchoType = parsed;
>>>>>>> origin/dev
            }
            else
            {
                throw new ArgumentException("Required field 'echoType'/'type' is missing or null in echo reveal reference");
            }
        }
        else
        {
            throw new ArgumentException("Required field 'echoType'/'type' is missing in echo reveal reference");
        }

        // Parse MinStrength (required)
        if (revealDict.TryGetValue("minStrength", out var minStrengthObj) ||
            revealDict.TryGetValue("min_strength", out minStrengthObj) ||
            revealDict.TryGetValue("threshold", out minStrengthObj))
        {
            if (minStrengthObj != null &&
                float.TryParse(minStrengthObj.ToString(), out float minStrength))
            {
                reveal.MinStrength = Math.Clamp(minStrength, 0.1f, 1.0f);
            }
            else
            {
                // Default to 0.5 if invalid
                reveal.MinStrength = 0.5f;
            }
        }
        else
        {
            // Default to 0.5 if not provided
            reveal.MinStrength = 0.5f;
        }

        // Parse TriggerSceneId (required)
        if (revealDict.TryGetValue("triggerSceneId", out var triggerSceneIdObj) ||
            revealDict.TryGetValue("trigger_scene_id", out triggerSceneIdObj) ||
            revealDict.TryGetValue("scene_id", out triggerSceneIdObj))
        {
            if (triggerSceneIdObj != null)
            {
                reveal.TriggerSceneId = triggerSceneIdObj.ToString() ?? string.Empty;
            }
            else
            {
                throw new ArgumentException("Required field 'triggerSceneId'/'scene_id' is missing or null in echo reveal reference");
            }
        }
        else
        {
            throw new ArgumentException("Required field 'triggerSceneId'/'scene_id' is missing in echo reveal reference");
        }

        // Parse RevealMechanic (optional, default "none")
        if (revealDict.TryGetValue("revealMechanic", out var revealMechanicObj) ||
            revealDict.TryGetValue("reveal_mechanic", out revealMechanicObj) ||
            revealDict.TryGetValue("mechanic", out revealMechanicObj))
        {
            if (revealMechanicObj != null)
            {
                string mechanic = revealMechanicObj.ToString()?.ToLower() ?? "none";
                // Validate that it's one of the allowed types
                if (mechanic == "mirror" || mechanic == "dream" || mechanic == "spirit" || mechanic == "none")
                {
                    reveal.RevealMechanic = mechanic;
                }
            }
        }

        // Parse MaxAgeScenes (optional, default 10)
        if (revealDict.TryGetValue("maxAgeScenes", out var maxAgeScenesObj) ||
            revealDict.TryGetValue("max_age_scenes", out maxAgeScenesObj) ||
            revealDict.TryGetValue("max_age", out maxAgeScenesObj))
        {
            if (maxAgeScenesObj != null &&
                int.TryParse(maxAgeScenesObj.ToString(), out int maxAgeScenes))
            {
                // Ensure positive value
                reveal.MaxAgeScenes = Math.Max(1, maxAgeScenes);
            }
        }

        // Parse Required (optional, default false)
        if (revealDict.TryGetValue("required", out var requiredObj) ||
            revealDict.TryGetValue("is_required", out requiredObj) ||
            revealDict.TryGetValue("mandatory", out requiredObj))
        {
            if (requiredObj != null)
            {
                if (requiredObj is bool boolValue)
                {
                    reveal.Required = boolValue;
                }
                else if (requiredObj.ToString()?.ToLower() is string stringValue)
                {
                    reveal.Required = stringValue == "true" || stringValue == "yes" || stringValue == "1";
                }
            }
        }

        return reveal;
    }
    private static SessionAchievement ParseSessionAchievement(IDictionary<object, object> achievementDict)
    {
        var achievement = new SessionAchievement();

        // Parse required string properties
        if (achievementDict.TryGetValue("id", out var idObj) && idObj != null)
        {
            achievement.Id = idObj.ToString() ?? string.Empty;
        }
        else
        {
            throw new ArgumentException("Required field 'id' is missing or null in session achievement data");
        }

        if (achievementDict.TryGetValue("description", out var descObj) && descObj != null)
        {
            achievement.Description = descObj.ToString() ?? string.Empty;
        }
        else
        {
            throw new ArgumentException("Required field 'description' is missing or null in session achievement data");
        }

        return achievement;
    }
}
