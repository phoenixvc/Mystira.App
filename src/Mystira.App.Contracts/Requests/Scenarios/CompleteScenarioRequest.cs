using System.ComponentModel.DataAnnotations;

namespace Mystira.App.Contracts.Requests.Scenarios;

public class CompleteScenarioRequest
{
    [Required]
    public string AccountId { get; set; } = string.Empty;

    [Required]
    public string ScenarioId { get; set; } = string.Empty;
}

