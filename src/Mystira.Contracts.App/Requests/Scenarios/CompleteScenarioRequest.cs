using System.ComponentModel.DataAnnotations;

namespace Mystira.Contracts.App.Requests.Scenarios;

public class CompleteScenarioRequest
{
    [Required]
    public string AccountId { get; set; } = string.Empty;

    [Required]
    public string ScenarioId { get; set; } = string.Empty;
}

