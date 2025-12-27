using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;
using Mystira.Contracts.App.Responses.Badges;

namespace Mystira.App.Application.CQRS.Badges.Queries;

/// <summary>
/// Wolverine handler for CalculateBadgeScoresQuery.
/// Calculates required badge scores per tier for all scenarios in a content bundle using depth-first traversal.
/// </summary>
public static class CalculateBadgeScoresQueryHandler
{
    /// <summary>
    /// Handles the CalculateBadgeScoresQuery by calculating per-axis percentile badge scores for all scenarios in a content bundle.
    /// Wolverine injects dependencies as method parameters.
    /// </summary>
    public static async Task<List<CompassAxisScoreResult>> Handle(
        CalculateBadgeScoresQuery query,
        IContentBundleRepository bundleRepository,
        IScenarioRepository scenarioRepository,
        ILogger logger,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query.ContentBundleId))
        {
            throw new ArgumentException("Content bundle ID cannot be null or empty", nameof(query.ContentBundleId));
        }

        if (query.Percentiles == null || !query.Percentiles.Any())
        {
            throw new ArgumentException("Percentiles array cannot be null or empty", nameof(query.Percentiles));
        }

        // Validate percentiles are in valid range
        if (query.Percentiles.Any(p => p < 0 || p > 100))
        {
            throw new ArgumentException("Percentiles must be between 0 and 100", nameof(query.Percentiles));
        }

        // Load the content bundle
        var bundle = await bundleRepository.GetByIdAsync(query.ContentBundleId);
        if (bundle == null)
        {
            throw new InvalidOperationException($"Content bundle not found: {query.ContentBundleId}");
        }

        logger.LogInformation(
            "Calculating badge scores for bundle {BundleId} with {ScenarioCount} scenarios",
            query.ContentBundleId,
            bundle.ScenarioIds.Count);

        // Load all scenarios in the bundle
        var scenarios = new List<Scenario>();
        foreach (var scenarioId in bundle.ScenarioIds)
        {
            var scenario = await scenarioRepository.GetByIdAsync(scenarioId);
            if (scenario != null)
            {
                scenarios.Add(scenario);
            }
            else
            {
                logger.LogWarning("Scenario {ScenarioId} not found in bundle {BundleId}", scenarioId, query.ContentBundleId);
            }
        }

        if (!scenarios.Any())
        {
            logger.LogWarning("No scenarios found for bundle {BundleId}", query.ContentBundleId);
            return new List<CompassAxisScoreResult>();
        }

        // Dictionary to store all path scores grouped by axis
        // Key: Axis name, Value: List of cumulative scores for all paths
        var axisPathScores = new Dictionary<string, List<double>>(StringComparer.OrdinalIgnoreCase);

        // Process each scenario
        foreach (var scenario in scenarios)
        {
            logger.LogDebug("Processing scenario {ScenarioId}: {Title}", scenario.Id, scenario.Title);

            // Perform depth-first traversal for each scenario
            var scenarioPaths = TraverseScenario(scenario);

            logger.LogDebug(
                "Found {PathCount} paths in scenario {ScenarioId}",
                scenarioPaths.Count,
                scenario.Id);

            // Aggregate scores by axis
            foreach (var path in scenarioPaths)
            {
                foreach (var (axis, score) in path)
                {
                    if (!axisPathScores.ContainsKey(axis))
                    {
                        axisPathScores[axis] = new List<double>();
                    }
                    axisPathScores[axis].Add(score);
                }
            }
        }

        // Calculate percentiles for each axis
        var results = new List<CompassAxisScoreResult>();

        foreach (var (axisName, scores) in axisPathScores)
        {
            if (!scores.Any())
            {
                continue;
            }

            var percentileScores = CalculatePercentiles(scores, query.Percentiles);

            results.Add(new CompassAxisScoreResult
            {
                AxisName = axisName,
                PercentileScores = percentileScores
            });

            logger.LogInformation(
                "Calculated percentiles for axis {AxisName}: {PathCount} paths analyzed",
                axisName,
                scores.Count);
        }

        logger.LogInformation(
            "Badge score calculation complete for bundle {BundleId}: {AxisCount} axes processed",
            query.ContentBundleId,
            results.Count);

        return results;
    }

    /// <summary>
    /// Performs depth-first traversal of a scenario graph and returns all possible paths
    /// with their cumulative compass axis scores.
    /// </summary>
    /// <param name="scenario">The scenario whose scenes will be traversed to generate paths.</param>
    /// <returns>A list where each item is a dictionary mapping axis names to their cumulative scores for a single path; returns an empty list if the scenario contains no scenes or produces no scored paths.</returns>
    private static List<Dictionary<string, double>> TraverseScenario(Scenario scenario)
    {
        var allPaths = new List<Dictionary<string, double>>();

        if (scenario.Scenes == null || !scenario.Scenes.Any())
        {
            return allPaths;
        }

        // Build scene lookup dictionary for efficient navigation
        var sceneDict = scenario.Scenes.ToDictionary(s => s.Id, s => s);

        // Find the starting scene (first scene or scene with no incoming references)
        var startScene = scenario.Scenes.FirstOrDefault();
        if (startScene == null)
        {
            return allPaths;
        }

        // Perform DFS from the start scene
        var visited = new HashSet<string>();
        var currentPath = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        DepthFirstSearch(startScene, sceneDict, visited, currentPath, allPaths);

        return allPaths;
    }

    /// <summary>
    /// Recursively traverses a scenario graph depth-first, accumulating compass-axis score totals for each explored path and adding complete path score maps to the collector.
    /// </summary>
    /// <param name="currentScene">The scene node currently being visited.</param>
    /// <param name="sceneDict">Lookup dictionary of scene ID to Scene used to follow NextSceneId and branch targets.</param>
    /// <param name="visited">Set of scene IDs already visited on the current traversal to detect cycles and avoid infinite recursion.</param>
    /// <param name="currentPath">Current cumulative axis-to-score map for the path being explored; branch explorations receive copies so sibling branches do not interfere.</param>
    /// <param name="allPaths">Collector that receives a copy of each completed path's axis score map.</param>
    private static void DepthFirstSearch(
        Scene currentScene,
        Dictionary<string, Scene> sceneDict,
        HashSet<string> visited,
        Dictionary<string, double> currentPath,
        List<Dictionary<string, double>> allPaths)
    {
        // Avoid infinite loops in circular graphs
        if (visited.Contains(currentScene.Id))
        {
            // Do NOT add to paths when a cycle is detected, as it's an incomplete path
            // from the perspective of badge scoring unless it hits a leaf.
            return;
        }

        visited.Add(currentScene.Id);

        // If scene has branches, explore each branch
        if (currentScene.Branches != null && currentScene.Branches.Any())
        {
            foreach (var branch in currentScene.Branches)
            {
                // Create a copy of the current path for this branch
                var branchPath = new Dictionary<string, double>(currentPath, StringComparer.OrdinalIgnoreCase);

                // Apply compass changes from this branch
                if (branch.CompassChange != null && !string.IsNullOrWhiteSpace(branch.CompassChange.Axis))
                {
                    var axis = branch.CompassChange.Axis;
                    if (!branchPath.ContainsKey(axis))
                    {
                        branchPath[axis] = 0;
                    }
                    branchPath[axis] += branch.CompassChange.Delta;
                }

                // If branch leads to another scene, continue traversal
                if (!string.IsNullOrWhiteSpace(branch.NextSceneId) && sceneDict.TryGetValue(branch.NextSceneId, out var nextScene))
                {
                    var branchVisited = new HashSet<string>(visited);
                    DepthFirstSearch(nextScene, sceneDict, branchVisited, branchPath, allPaths);
                }
                else
                {
                    // End of path - save it
                    if (branchPath.Any())
                    {
                        allPaths.Add(branchPath);
                    }
                }
            }
        }
        else
        {
            // No branches - check if there's a direct next scene
            if (!string.IsNullOrWhiteSpace(currentScene.NextSceneId) && sceneDict.ContainsKey(currentScene.NextSceneId))
            {
                var nextScene = sceneDict[currentScene.NextSceneId];
                DepthFirstSearch(nextScene, sceneDict, visited, currentPath, allPaths);
            }
            else
            {
                // End of path - save it
                if (currentPath.Any())
                {
                    allPaths.Add(new Dictionary<string, double>(currentPath, StringComparer.OrdinalIgnoreCase));
                }
            }
        }
    }

    /// <summary>
    /// Computes the requested percentile values from a collection of numeric scores.
    /// </summary>
    /// <param name="scores">The list of scores to compute percentiles from; order is irrelevant. If empty, an empty dictionary is returned.</param>
    /// <param name="percentiles">The percentiles to compute, expressed as numbers between 0 and 100 (inclusive).</param>
    /// <returns>Mapping from each requested percentile to its corresponding score value.</returns>
    private static Dictionary<double, double> CalculatePercentiles(List<double> scores, List<double> percentiles)
    {
        var result = new Dictionary<double, double>();

        if (!scores.Any())
        {
            return result;
        }

        // Sort scores for percentile calculation
        var sortedScores = scores.OrderBy(s => s).ToList();

        foreach (var percentile in percentiles)
        {
            var score = CalculatePercentile(sortedScores, percentile);
            result[percentile] = score;
        }

        return result;
    }

    /// <summary>
    /// Computes the value at the specified percentile from a list of ascending-sorted scores using linear interpolation.
    /// </summary>
    /// <param name="sortedScores">A list of scores sorted in ascending order.</param>
    /// <param name="percentile">The percentile to compute, expressed from 0 to 100.</param>
    /// <returns>The interpolated score at the requested percentile; returns 0 if the list is empty, or the single element if the list contains one value.</returns>
    private static double CalculatePercentile(List<double> sortedScores, double percentile)
    {
        if (!sortedScores.Any())
        {
            return 0;
        }

        if (sortedScores.Count == 1)
        {
            return sortedScores[0];
        }

        // Calculate the position in the sorted array
        var position = (percentile / 100.0) * (sortedScores.Count - 1);
        var lowerIndex = (int)Math.Floor(position);
        var upperIndex = (int)Math.Ceiling(position);

        // Handle edge cases
        if (lowerIndex < 0)
        {
            return sortedScores[0];
        }

        if (upperIndex >= sortedScores.Count)
        {
            return sortedScores[sortedScores.Count - 1];
        }

        if (lowerIndex == upperIndex)
        {
            return sortedScores[lowerIndex];
        }

        // Linear interpolation between two values
        var lowerValue = sortedScores[lowerIndex];
        var upperValue = sortedScores[upperIndex];
        var fraction = position - lowerIndex;

        return lowerValue + (upperValue - lowerValue) * fraction;
    }
}
