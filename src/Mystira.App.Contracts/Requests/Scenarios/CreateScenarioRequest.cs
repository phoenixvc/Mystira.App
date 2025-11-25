using System.ComponentModel.DataAnnotations;
using Mystira.App.Domain.Models;

namespace Mystira.App.Contracts.Requests.Scenarios;

public class CreateScenarioRequest
{
    [Required]
    [StringLength(200, MinimumLength = 5)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(1000, MinimumLength = 10)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public List<string> Tags { get; set; } = new();

    [Required]
    public DifficultyLevel Difficulty { get; set; }

    [Required]
    public SessionLength SessionLength { get; set; }

    [Required]
    [MaxLength(4)]
    public List<string> Archetypes { get; set; } = new();

    [Required]
    public string AgeGroup { get; set; } = string.Empty;

    [Required]
    public int MinimumAge { get; set; }

    [Required]
    [MaxLength(4)]
    public List<string> CoreAxes { get; set; } = new();

    [Required]
    public List<ScenarioCharacter> Characters { get; set; } = new();

    [Required]
    public List<Scene> Scenes { get; set; } = new();

    public List<string> CompassAxes { get; set; } = new();
}

