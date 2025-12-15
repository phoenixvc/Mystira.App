using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.GameSessions.Commands;

public record FinalizeGameSessionCommand(string SessionId) : ICommand<FinalizeGameSessionResult>;

public class FinalizeGameSessionResult
{
    public string SessionId { get; set; } = string.Empty;
    public List<ProfileBadgeAwards> Awards { get; set; } = new();
}

public class ProfileBadgeAwards
{
    public string ProfileId { get; set; } = string.Empty;
    public string? ProfileName { get; set; }
    public List<UserBadge> NewBadges { get; set; } = new();
}
