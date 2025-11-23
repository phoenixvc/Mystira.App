using System.ComponentModel.DataAnnotations;

namespace Mystira.App.Contracts.Requests.GameSessions;

public class StartGameSessionRequest
{
    [Required]
    public string ScenarioId { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string AccountId { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string ProfileId { get; set; } = string.Empty;

    [Required]
    public List<string> PlayerNames { get; set; } = new();

    [Required]
    public string TargetAgeGroup { get; set; } = string.Empty;
}

