using System.ComponentModel.DataAnnotations;

namespace Mystira.Contracts.App.Requests.GameSessions;

public class MakeChoiceRequest
{
    [Required]
    public string SessionId { get; set; } = string.Empty;

    [Required]
    public string SceneId { get; set; } = string.Empty;

    [Required]
    public string ChoiceText { get; set; } = string.Empty;

    [Required]
    public string NextSceneId { get; set; } = string.Empty;

    public string? PlayerId { get; set; }

    public string? CompassAxis { get; set; }
    public string? CompassDirection { get; set; }
    public double? CompassDelta { get; set; }
}
