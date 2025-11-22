using Microsoft.Extensions.Logging;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.UseCases.Scenarios;

/// <summary>
/// Use case for validating scenario business rules
/// </summary>
public class ValidateScenarioUseCase
{
    private readonly ILogger<ValidateScenarioUseCase> _logger;

    public ValidateScenarioUseCase(ILogger<ValidateScenarioUseCase> logger)
    {
        _logger = logger;
    }

    public Task ExecuteAsync(Scenario scenario)
    {
        // Validate scene references
        var sceneIds = scenario.Scenes.Select(s => s.Id).ToHashSet();
        var allReferencedScenes = new HashSet<string>();

        foreach (var scene in scenario.Scenes)
        {
            // Check next_scene references
            if (!string.IsNullOrEmpty(scene.NextSceneId))
            {
                if (!sceneIds.Contains(scene.NextSceneId))
                {
                    throw new ArgumentException($"Scene '{scene.Id}' references non-existent next scene '{scene.NextSceneId}'");
                }

                allReferencedScenes.Add(scene.NextSceneId);
            }

            // Check branch references
            if (scene.Branches != null)
            {
                foreach (var branch in scene.Branches)
                {
                    if (!sceneIds.Contains(branch.NextSceneId))
                    {
                        throw new ArgumentException($"Scene '{scene.Id}' branch references non-existent scene '{branch.NextSceneId}'");
                    }

                    allReferencedScenes.Add(branch.NextSceneId);
                }
            }

            // Check echo reveal references
            if (scene.EchoReveals != null)
            {
                foreach (var reveal in scene.EchoReveals)
                {
                    if (!sceneIds.Contains(reveal.TriggerSceneId))
                    {
                        throw new ArgumentException($"Scene '{scene.Id}' echo reveal references non-existent scene '{reveal.TriggerSceneId}'");
                    }
                }
            }
        }

        // Validate that all scenes are reachable (except the first scene)
        var firstScene = scenario.Scenes.FirstOrDefault();
        if (firstScene == null)
        {
            throw new ArgumentException("Scenario must have at least one scene");
        }

        // Check for unreachable scenes (scenes that are never referenced)
        var unreachableScenes = sceneIds.Except(allReferencedScenes).Where(id => id != firstScene.Id).ToList();
        if (unreachableScenes.Count > 0)
        {
            _logger.LogWarning("Scenario '{ScenarioId}' has unreachable scenes: {UnreachableScenes}",
                scenario.Id, string.Join(", ", unreachableScenes));
        }

        // Note: Character references in scenes are not currently stored in the Scene model
        // This validation would need to be added if scenes reference characters directly

        return Task.CompletedTask;
    }
}

