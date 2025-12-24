using Mystira.Contracts.App.Models.GameSessions;
using Mystira.App.Domain.Models;

namespace Mystira.Contracts.App.Responses.GameSessions;

public class SessionStatsResponse
{
    public Dictionary<string, double> CompassValues { get; set; } = new();
    public List<PlayerCompassProgressDto> PlayerCompassProgressTotals { get; set; } = new();
    public List<EchoLog> RecentEchoes { get; set; } = new();
    public List<SessionAchievement> Achievements { get; set; } = new();
    public int TotalChoices { get; set; }
    public TimeSpan SessionDuration { get; set; }
}
