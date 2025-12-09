using Mystira.App.Domain.Models;

namespace Mystira.App.Application.Parsers;

/// <summary>
/// Parser for converting branch dictionary data to Branch domain object
/// </summary>
public static class BranchParser
{
    public static Branch Parse(IDictionary<object, object> branchDict)
    {
        var branch = new Branch();

        // Parse required Choice field (replaces Text field)
        var choiceFound = branchDict.TryGetValue("choice", out var choiceObj) ||
                          branchDict.TryGetValue("text", out choiceObj) ||
                          branchDict.TryGetValue("option", out choiceObj);

        if (!choiceFound || choiceObj == null)
        {
            throw new ArgumentException("Required field 'choice'/'text' is missing or null in branch data");
        }
        branch.Choice = choiceObj.ToString() ?? string.Empty;

        // Parse optional NextSceneId field (null/empty means story ending)
        var nextSceneFound = branchDict.TryGetValue("nextSceneId", out var nextSceneObj) ||
                             branchDict.TryGetValue("next_scene_id", out nextSceneObj) ||
                             branchDict.TryGetValue("next_scene", out nextSceneObj);

        // NextSceneId is optional - if not found or null, the branch ends the story
        var nextScene = nextSceneFound && nextSceneObj != null ? nextSceneObj.ToString() : null;
        branch.NextSceneId = string.IsNullOrWhiteSpace(nextScene) ? string.Empty : nextScene;

        // Parse EchoLog if available
        if ((branchDict.TryGetValue("echoLog", out var echoLogObj) ||
             branchDict.TryGetValue("echo_log", out echoLogObj)) &&
            echoLogObj is IDictionary<object, object> echoLogDict)
        {
            branch.EchoLog = EchoLogParser.Parse(echoLogDict);
        }

        // Parse CompassChange if available
        if ((branchDict.TryGetValue("compassChange", out var compassChangeObj) ||
             branchDict.TryGetValue("compass_change", out compassChangeObj) ||
             branchDict.TryGetValue("compass_impact", out compassChangeObj)) &&
            compassChangeObj is IDictionary<object, object> compassChangeDict)
        {
            branch.CompassChange = CompassChangeParser.Parse(compassChangeDict);
        }

        return branch;
    }
}

