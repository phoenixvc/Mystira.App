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
                branch.EchoLog = EchoLogParser.Parse(echoLogDict);
            }
        }

        // Parse CompassChange if available
        if (branchDict.TryGetValue("compassChange", out var compassChangeObj) ||
            branchDict.TryGetValue("compass_change", out compassChangeObj) ||
            branchDict.TryGetValue("compass_impact", out compassChangeObj))
        {
            if (compassChangeObj is IDictionary<object, object> compassChangeDict)
            {
                branch.CompassChange = CompassChangeParser.Parse(compassChangeDict);
            }
        }

        return branch;
    }
}

