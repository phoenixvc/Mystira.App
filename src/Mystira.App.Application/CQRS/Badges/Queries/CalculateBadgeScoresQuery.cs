using Mystira.Contracts.App.Responses.Badges;
using Mystira.Shared.CQRS;

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
