using System.ComponentModel.DataAnnotations;
using Mystira.Contracts.App.Models.GameSessions;

namespace Mystira.Contracts.App.Requests.GameSessions;

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

    // Either PlayerNames or CharacterAssignments should be provided
    public List<string> PlayerNames { get; set; } = new();

    public List<CharacterAssignmentDto> CharacterAssignments { get; set; } = new();

    [Required]
    public string TargetAgeGroup { get; set; } = string.Empty;
}

