using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.Domain.Models;

namespace Mystira.App.Application.UseCases.Scenarios;

/// <summary>
/// Use case for validating scenario business rules
/// </summary>
public class ValidateScenarioUseCase
{
    private readonly ILogger<ValidateScenarioUseCase> _logger;
    private readonly ICompassAxisRepository _compassAxisRepository;
    private readonly IArchetypeRepository _archetypeRepository;

    public ValidateScenarioUseCase(
        ILogger<ValidateScenarioUseCase> logger,
        ICompassAxisRepository compassAxisRepository,
        IArchetypeRepository archetypeRepository)
    {
        _logger = logger;
        _compassAxisRepository = compassAxisRepository;
        _archetypeRepository = archetypeRepository;
    }

    public async Task ExecuteAsync(Scenario scenario)
    {
        // Validate CoreAxes against DB
        if (scenario.CoreAxes != null && scenario.CoreAxes.Count > 0)
        {
            var validAxes = await _compassAxisRepository.GetAllAsync();
            var validAxisNames = validAxes.Select(a => a.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var axis in scenario.CoreAxes)
            {
                if (!validAxisNames.Contains(axis.Value))
                {
                    throw new ArgumentException($"Invalid compass axis: '{axis.Value}'. Valid values: {string.Join(", ", validAxisNames)}");
                }
            }
        }

        // Validate Archetypes against DB
        if (scenario.Archetypes != null && scenario.Archetypes.Count > 0)
        {
            var validArchetypes = await _archetypeRepository.GetAllAsync();
            var validArchetypeNames = validArchetypes.Select(a => a.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var archetype in scenario.Archetypes)
            {
                if (!validArchetypeNames.Contains(archetype.Value))
                {
                    throw new ArgumentException($"Invalid archetype: '{archetype.Value}'. Valid values: {string.Join(", ", validArchetypeNames)}");
                }
            }
        }

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
            // Note: Empty NextSceneId is valid - it means the branch ends the story
            if (scene.Branches != null)
            {
                foreach (var branch in scene.Branches)
                {
                    // Skip validation for empty NextSceneId (story ending)
                    if (string.IsNullOrEmpty(branch.NextSceneId))
                    {
                        continue;
                    }

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
        // Additional validation: For choice scenes, ActiveCharacter must match a Scenario.Character id.
        if (scenario.Scenes != null && scenario.Characters != null)
        {
            var characterIds = scenario.Characters.Select(c => c.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var scene in scenario.Scenes)
            {
                if (scene.Type == SceneType.Choice)
                {
                    if (string.IsNullOrWhiteSpace(scene.ActiveCharacter))
                    {
                        _logger.LogError(
                            "Scenario '{ScenarioId}' scene '{SceneId}' is a choice scene but has no active_character set.",
                            scenario.Id, scene.Id);
                    }
                    else if (!characterIds.Contains(scene.ActiveCharacter))
                    {
                        _logger.LogError(
                            "Scenario '{ScenarioId}' scene '{SceneId}' has active_character '{ActiveCharacter}' that does not match any scenario character ids.",
                            scenario.Id, scene.Id, scene.ActiveCharacter);
                    }
                }
            }
        }
    }
}

