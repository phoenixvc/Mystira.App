using Mystira.App.Domain.Models;

namespace Mystira.App.Api.Models.Parsers;

/// <summary>
/// Parser for converting echo reveal dictionary data to EchoReveal domain object
/// </summary>
public static class EchoRevealParser
{
    public static EchoReveal Parse(IDictionary<object, object> revealDict)
    {
        var reveal = new EchoReveal();

        // Parse EchoType (required)
        if (revealDict.TryGetValue("echoType", out var echoTypeObj) ||
            revealDict.TryGetValue("echo_type", out echoTypeObj) ||
            revealDict.TryGetValue("type", out echoTypeObj))
        {
            if (echoTypeObj != null)
            {
                var parsed = EchoType.Parse(echoTypeObj.ToString());
                if (parsed == null)
                {
                    throw new ArgumentException($"Invalid EchoType: {echoTypeObj}");
                }

                reveal.EchoType = parsed;
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
}

