namespace Mystira.App.Application.CQRS.Badges.Queries;

/// <summary>
/// Query to calculate required badge scores per tier for a content bundle.
/// Performs depth-first traversal of all scenarios in the bundle and calculates
/// percentile-based score thresholds for each compass axis.
/// </summary>
public record CalculateBadgeScoresQuery(
    string ContentBundleId,
    List<double> Percentiles
) : IQuery<List<CompassAxisScoreResult>>;

/// <summary>
/// Represents the score calculation result for a single compass axis.
/// Contains percentile-to-score mappings.
/// </summary>
public class CompassAxisScoreResult
{
    /// <summary>
    /// The name of the compass axis (e.g., "Courage", "Wisdom")
    /// </summary>
    public string AxisName { get; set; } = string.Empty;

    /// <summary>
    /// Dictionary mapping percentile values to required scores.
    /// Key: Percentile (e.g., 50, 75, 90)
    /// Value: Required score to achieve that percentile
    /// </summary>
    public Dictionary<double, double> PercentileScores { get; set; } = new();
}
