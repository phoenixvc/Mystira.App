using Mystira.App.Domain.Models;

namespace Mystira.App.Application.Parsers;

/// <summary>
/// Parser for converting echo reveal dictionary data to EchoReveal domain object
/// </summary>
public static class EchoRevealParser
{
    public static EchoReveal Parse(IDictionary<object, object> revealDict)
    {
        var reveal = new EchoReveal();

        // Parse EchoType (required)
        var echoTypeFound = revealDict.TryGetValue("echoType", out var echoTypeObj) ||
                            revealDict.TryGetValue("echo_type", out echoTypeObj) ||
                            revealDict.TryGetValue("type", out echoTypeObj);

        if (!echoTypeFound || echoTypeObj == null)
        {
            throw new ArgumentException("Required field 'echoType'/'type' is missing or null in echo reveal reference");
        }

        reveal.EchoType = echoTypeObj.ToString() ?? string.Empty;

        // Parse MinStrength (optional, default 0.5)
        var minStrengthFound = revealDict.TryGetValue("minStrength", out var minStrengthObj) ||
                               revealDict.TryGetValue("min_strength", out minStrengthObj) ||
                               revealDict.TryGetValue("threshold", out minStrengthObj);

        reveal.MinStrength = minStrengthFound && minStrengthObj != null &&
                             float.TryParse(minStrengthObj.ToString(), out float minStrength)
            ? Math.Clamp(minStrength, 0.1f, 1.0f)
            : 0.5f;

        // Parse TriggerSceneId (required)
        var triggerSceneFound = revealDict.TryGetValue("triggerSceneId", out var triggerSceneIdObj) ||
                                revealDict.TryGetValue("trigger_scene_id", out triggerSceneIdObj) ||
                                revealDict.TryGetValue("scene_id", out triggerSceneIdObj);

        if (!triggerSceneFound || triggerSceneIdObj == null)
        {
            throw new ArgumentException("Required field 'triggerSceneId'/'scene_id' is missing or null in echo reveal reference");
        }
        reveal.TriggerSceneId = triggerSceneIdObj.ToString() ?? string.Empty;

        // Parse RevealMechanic (optional, default "none")
        if ((revealDict.TryGetValue("revealMechanic", out var revealMechanicObj) ||
             revealDict.TryGetValue("reveal_mechanic", out revealMechanicObj) ||
             revealDict.TryGetValue("mechanic", out revealMechanicObj)) &&
            revealMechanicObj != null)
        {
            string mechanic = revealMechanicObj.ToString()?.ToLower() ?? "none";
            // Validate that it's one of the allowed types
            if (mechanic is "mirror" or "dream" or "spirit" or "none")
            {
                reveal.RevealMechanic = mechanic;
            }
        }

        // Parse MaxAgeScenes (optional, default 10)
        if ((revealDict.TryGetValue("maxAgeScenes", out var maxAgeScenesObj) ||
             revealDict.TryGetValue("max_age_scenes", out maxAgeScenesObj) ||
             revealDict.TryGetValue("max_age", out maxAgeScenesObj)) &&
            maxAgeScenesObj != null &&
            int.TryParse(maxAgeScenesObj.ToString(), out int maxAgeScenes))
        {
            // Ensure positive value
            reveal.MaxAgeScenes = Math.Max(1, maxAgeScenes);
        }

        // Parse Required (optional, default false)
        if ((revealDict.TryGetValue("required", out var requiredObj) ||
             revealDict.TryGetValue("is_required", out requiredObj) ||
             revealDict.TryGetValue("mandatory", out requiredObj)) &&
            requiredObj != null)
        {
            reveal.Required = requiredObj is bool boolValue
                ? boolValue
                : requiredObj.ToString()?.ToLower() is "true" or "yes" or "1";
        }

        return reveal;
    }
}

