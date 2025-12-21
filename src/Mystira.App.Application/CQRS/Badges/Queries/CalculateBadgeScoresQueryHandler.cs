using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.Badges.Queries;

/// <summary>
/// Handler for CalculateBadgeScoresQuery - calculates required badge scores per tier
/// for all scenarios in a content bundle using depth-first traversal.
/// </summary>
public class CalculateBadgeScoresQueryHandler : IQueryHandler<CalculateBadgeScoresQuery, List<CompassAxisScoreResult>>
{
    private readonly IContentBundleRepository _bundleRepository;
    private readonly IScenarioRepository _scenarioRepository;
    private readonly ILogger<CalculateBadgeScoresQueryHandler> _logger;

    public CalculateBadgeScoresQueryHandler(
        IContentBundleRepository bundleRepository,
        IScenarioRepository scenarioRepository,
        ILogger<CalculateBadgeScoresQueryHandler> logger)
    {
        _bundleRepository = bundleRepository;
        _scenarioRepository = scenarioRepository;
        _logger = logger;
    }

    public async Task<List<CompassAxisScoreResult>> Handle(
        CalculateBadgeScoresQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ContentBundleId))
        {
            throw new ArgumentException("Content bundle ID cannot be null or empty", nameof(request.ContentBundleId));
        }

        if (request.Percentiles == null || !request.Percentiles.Any())
        {
            throw new ArgumentException("Percentiles array cannot be null or empty", nameof(request.Percentiles));
        }

        // Validate percentiles are in valid range
        if (request.Percentiles.Any(p => p < 0 || p > 100))
        {
            throw new ArgumentException("Percentiles must be between 0 and 100", nameof(request.Percentiles));
        }

        // Load the content bundle
        var bundle = await _bundleRepository.GetByIdAsync(request.ContentBundleId);
        if (bundle == null)
        {
            throw new InvalidOperationException($"Content bundle not found: {request.ContentBundleId}");
        }

        _logger.LogInformation(
            "Calculating badge scores for bundle {BundleId} with {ScenarioCount} scenarios",
            request.ContentBundleId,
            bundle.ScenarioIds.Count);

        // Load all scenarios in the bundle
        var scenarios = new List<Scenario>();
        foreach (var scenarioId in bundle.ScenarioIds)
        {
            var scenario = await _scenarioRepository.GetByIdAsync(scenarioId);
            if (scenario != null)
            {
                scenarios.Add(scenario);
            }
            else
            {
                _logger.LogWarning("Scenario {ScenarioId} not found in bundle {BundleId}", scenarioId, request.ContentBundleId);
            }
        }

        if (!scenarios.Any())
        {
            _logger.LogWarning("No scenarios found for bundle {BundleId}", request.ContentBundleId);
            return new List<CompassAxisScoreResult>();
        }

        // Dictionary to store all path scores grouped by axis
        // Key: Axis name, Value: List of cumulative scores for all paths
        var axisPathScores = new Dictionary<string, List<double>>(StringComparer.OrdinalIgnoreCase);

        // Process each scenario
        foreach (var scenario in scenarios)
        {
            _logger.LogDebug("Processing scenario {ScenarioId}: {Title}", scenario.Id, scenario.Title);

            // Perform depth-first traversal for each scenario
            var scenarioPaths = TraverseScenario(scenario);

            _logger.LogDebug(
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

            var percentileScores = CalculatePercentiles(scores, request.Percentiles);

            results.Add(new CompassAxisScoreResult
            {
                AxisName = axisName,
                PercentileScores = percentileScores
            });

            _logger.LogInformation(
                "Calculated percentiles for axis {AxisName}: {PathCount} paths analyzed",
                axisName,
                scores.Count);
        }

        _logger.LogInformation(
            "Badge score calculation complete for bundle {BundleId}: {AxisCount} axes processed",
            request.ContentBundleId,
            results.Count);

        return results;
    }

    /// <summary>
    /// Performs depth-first traversal of a scenario graph and returns all possible paths
    /// with their cumulative compass axis scores.
    /// </summary>
    /// <param name="scenario">The scenario to traverse</param>
    /// <returns>List of paths, where each path is a dictionary of axis names to cumulative scores</returns>
    private List<Dictionary<string, double>> TraverseScenario(Scenario scenario)
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
    /// Recursive depth-first search through the scenario graph.
    /// </summary>
    private void DepthFirstSearch(
        Scene currentScene,
        Dictionary<string, Scene> sceneDict,
        HashSet<string> visited,
        Dictionary<string, double> currentPath,
        List<Dictionary<string, double>> allPaths)
    {
        // Avoid infinite loops in circular graphs
        if (visited.Contains(currentScene.Id))
        {
            // Save current path when we hit a cycle or revisit
            if (currentPath.Any())
            {
                allPaths.Add(new Dictionary<string, double>(currentPath, StringComparer.OrdinalIgnoreCase));
            }
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
                if (!string.IsNullOrWhiteSpace(branch.NextSceneId) && sceneDict.ContainsKey(branch.NextSceneId))
                {
                    var nextScene = sceneDict[branch.NextSceneId];
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
    /// Calculates percentile values from a list of scores.
    /// Uses linear interpolation for percentile calculation.
    /// </summary>
    /// <param name="scores">List of scores to analyze</param>
    /// <param name="percentiles">List of percentile values to calculate (0-100)</param>
    /// <returns>Dictionary mapping percentile to score value</returns>
    private Dictionary<double, double> CalculatePercentiles(List<double> scores, List<double> percentiles)
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
    /// Calculates a specific percentile value using linear interpolation.
    /// </summary>
    private double CalculatePercentile(List<double> sortedScores, double percentile)
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
