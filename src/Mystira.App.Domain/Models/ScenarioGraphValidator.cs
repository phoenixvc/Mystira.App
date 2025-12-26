namespace Mystira.App.Domain.Models;

/// <summary>
/// Service to validate the structural integrity of a Scenario graph.
/// Detects infinite loops (cycles) and unreachable scenes.
/// </summary>
public class ScenarioGraphValidator
{
    /// <summary>
    /// Validates a scenario graph for cycles and reachability issues.
    /// </summary>
    /// <param name="scenario">The scenario to validate.</param>
    /// <param name="errors">Output list of validation errors.</param>
    /// <returns>True if the graph is valid, false otherwise.</returns>
    public bool ValidateGraph(Scenario scenario, out List<string> errors)
    {
        errors = new List<string>();

        if (scenario.Scenes == null || !scenario.Scenes.Any())
        {
            return true; // Basic validation handled by Scenario.Validate()
        }

        var sceneDict = scenario.Scenes.ToDictionary(s => s.Id, s => s);
        var startScene = scenario.Scenes.FirstOrDefault();

        if (startScene == null) return true;

        var visited = new HashSet<string>();
        var recursionStack = new HashSet<string>();
        var unreachableScenes = new HashSet<string>(sceneDict.Keys);

        // 1. Detect Cycles and Track Reachability
        DetectCycles(startScene.Id, sceneDict, visited, recursionStack, unreachableScenes, errors);

        // 2. Report Unreachable Scenes
        if (unreachableScenes.Any())
        {
            foreach (var sceneId in unreachableScenes)
            {
                errors.Add($"Scene '{sceneDict[sceneId].Title}' (ID: {sceneId}) is unreachable from the start scene.");
            }
        }

        // TODO: Production Hardening - Add validation for "dead ends" (scenes with no next scene and no branches that aren't marked as endings)

        return !errors.Any();
    }

    private void DetectCycles(
        string currentId,
        Dictionary<string, Scene> sceneDict,
        HashSet<string> visited,
        HashSet<string> recursionStack,
        HashSet<string> unreachableScenes,
        List<string> errors)
    {
        visited.Add(currentId);
        recursionStack.Add(currentId);
        unreachableScenes.Remove(currentId);

        if (sceneDict.TryGetValue(currentId, out var scene))
        {
            var nextIds = new List<string>();
            if (!string.IsNullOrEmpty(scene.NextSceneId)) nextIds.Add(scene.NextSceneId);
            if (scene.Branches != null)
            {
                nextIds.AddRange(scene.Branches.Where(b => !string.IsNullOrEmpty(b.NextSceneId)).Select(b => b.NextSceneId));
            }

            foreach (var nextId in nextIds)
            {
                if (recursionStack.Contains(nextId))
                {
                    errors.Add($"Infinite loop detected: Scene '{scene.Title}' leads back to '{sceneDict[nextId].Title}' which is already in the current path.");
                }
                else if (!visited.Contains(nextId) && sceneDict.ContainsKey(nextId))
                {
                    DetectCycles(nextId, sceneDict, visited, recursionStack, unreachableScenes, errors);
                }
            }
        }

        recursionStack.Remove(currentId);
    }
}
