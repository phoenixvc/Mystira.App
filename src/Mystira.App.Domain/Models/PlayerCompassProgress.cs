namespace Mystira.App.Domain.Models;

public class PlayerCompassProgress
{
    public string PlayerId { get; set; } = string.Empty;
    public string Axis { get; set; } = string.Empty;
    public double Total { get; set; }
}
