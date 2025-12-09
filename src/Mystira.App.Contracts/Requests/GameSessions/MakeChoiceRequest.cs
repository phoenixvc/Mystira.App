using System.ComponentModel.DataAnnotations;

namespace Mystira.App.Contracts.Requests.GameSessions;

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
}

