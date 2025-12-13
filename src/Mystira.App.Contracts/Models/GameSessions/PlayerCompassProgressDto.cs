namespace Mystira.App.Contracts.Models.GameSessions;

public class PlayerCompassProgressDto
{
    public string PlayerId { get; set; } = string.Empty;
    public string Axis { get; set; } = string.Empty;
    public double Total { get; set; }
}
