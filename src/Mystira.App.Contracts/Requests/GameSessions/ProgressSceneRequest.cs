using System.ComponentModel.DataAnnotations;

namespace Mystira.App.Contracts.Requests.GameSessions;

public class ProgressSceneRequest
{
    public string SessionId { get; set; } = string.Empty;

    [Required]
    public string SceneId { get; set; } = string.Empty;
}

