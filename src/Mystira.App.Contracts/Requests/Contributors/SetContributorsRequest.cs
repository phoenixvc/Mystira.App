using System.ComponentModel.DataAnnotations;

namespace Mystira.App.Contracts.Requests.Contributors;

/// <summary>
/// Request to set multiple contributors for a scenario or bundle at once
/// </summary>
public class SetContributorsRequest
{
    /// <summary>
    /// List of contributors with their revenue shares
    /// Percentages must sum to 100%
    /// </summary>
    [Required]
    [MinLength(1, ErrorMessage = "At least one contributor is required")]
    public List<ContributorRequest> Contributors { get; set; } = new();
}
