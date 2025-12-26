using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Application.Services;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.GameSessions.Commands;

/// <summary>
/// Wolverine handler for FinalizeGameSessionCommand.
/// Finalizes a game session by scoring scenarios and awarding badges to participating profiles.
/// Uses static method convention for cleaner, more testable code.
/// </summary>
public static class FinalizeGameSessionCommandHandler
{
    /// <summary>
    /// Handles the FinalizeGameSessionCommand.
    /// Wolverine injects dependencies as method parameters.
    /// </summary>
    public static async Task<FinalizeGameSessionResult> Handle(
        FinalizeGameSessionCommand command,
        IGameSessionRepository sessionRepository,
        IUserProfileRepository profileRepository,
        IPlayerScenarioScoreRepository scoreRepository,
        IAxisScoringService scoringService,
        IBadgeAwardingService badgeService,
        ILogger logger,
        CancellationToken ct)
    {
        var result = new FinalizeGameSessionResult { SessionId = command.SessionId };

        var session = await sessionRepository.GetByIdAsync(command.SessionId);
        if (session == null)
        {
            logger.LogWarning("Session not found for finalize: {SessionId}", command.SessionId);
            return result;
        }

        // Determine participating profiles
        var profileIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(session.ProfileId))
        {
            profileIds.Add(session.ProfileId);
        }
        foreach (var assignment in session.CharacterAssignments)
        {
            var pid = assignment.PlayerAssignment?.ProfileId;
            if (!string.IsNullOrWhiteSpace(pid))
            {
                profileIds.Add(pid);
            }
        }

        foreach (var profileId in profileIds)
        {
            var profile = await profileRepository.GetByIdAsync(profileId);
            if (profile == null)
            {
                logger.LogWarning("Profile {ProfileId} not found while finalizing session {SessionId}", profileId, session.Id);
                continue;
            }

            // Score first-time plays only (service skips if already scored)
            PlayerScenarioScore? score = await scoringService.ScoreSessionAsync(session, profile);
            var alreadyPlayed = score == null;

            // Compute cumulative axis totals across all scored scenarios for this profile
            var allScores = await scoreRepository.GetByProfileIdAsync(profile.Id);
            var cumulative = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in allScores)
            {
                foreach (var kv in s.AxisScores)
                {
                    if (!cumulative.ContainsKey(kv.Key)) cumulative[kv.Key] = 0f;
                    cumulative[kv.Key] += kv.Value;
                }
            }

            // Award badges based on cumulative totals (will no-op for already-earned badges)
            var newBadges = await badgeService.AwardBadgesAsync(profile, cumulative);

            // Always include an entry so the client can show players who did/didn't receive a badge
            result.Awards.Add(new ProfileBadgeAwards
            {
                ProfileId = profile.Id,
                ProfileName = profile.Name,
                NewBadges = newBadges,
                AlreadyPlayed = alreadyPlayed
            });
        }

        logger.LogInformation("Finalized session {SessionId}. New badge awards for {Count} profile(s).",
            session.Id, result.Awards.Count);

        return result;
    }
}
