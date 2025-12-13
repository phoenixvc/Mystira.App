using Mystira.App.PWA.Models;

namespace Mystira.App.PWA.Services;

public interface IAchievementsService
{
    Task<AchievementsLoadResult> GetAchievementsAsync(UserProfile profile, CancellationToken cancellationToken = default);
}

public class AchievementsLoadResult
{
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public AchievementsViewModel? Data { get; init; }

    public static AchievementsLoadResult Success(AchievementsViewModel data) => new() { IsSuccess = true, Data = data };
    public static AchievementsLoadResult Fail(string errorMessage) => new() { IsSuccess = false, ErrorMessage = errorMessage };
}
