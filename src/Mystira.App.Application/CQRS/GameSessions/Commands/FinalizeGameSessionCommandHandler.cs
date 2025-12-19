using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Application.Services;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.GameSessions.Commands;

public class FinalizeGameSessionCommandHandler : ICommandHandler<FinalizeGameSessionCommand, FinalizeGameSessionResult>
{
    private readonly IGameSessionRepository _sessionRepository;
    private readonly IUserProfileRepository _profileRepository;
    private readonly IPlayerScenarioScoreRepository _scoreRepository;
    private readonly IAxisScoringService _scoringService;
    private readonly IBadgeAwardingService _badgeService;
    private readonly ILogger<FinalizeGameSessionCommandHandler> _logger;

    public FinalizeGameSessionCommandHandler(
        IGameSessionRepository sessionRepository,
        IUserProfileRepository profileRepository,
        IPlayerScenarioScoreRepository scoreRepository,
        IAxisScoringService scoringService,
        IBadgeAwardingService badgeService,
        ILogger<FinalizeGameSessionCommandHandler> logger)
    {
        _sessionRepository = sessionRepository;
        _profileRepository = profileRepository;
        _scoreRepository = scoreRepository;
        _scoringService = scoringService;
        _badgeService = badgeService;
        _logger = logger;
    }

    public async Task<FinalizeGameSessionResult> Handle(
        FinalizeGameSessionCommand command,
        CancellationToken cancellationToken)
    {
        var result = new FinalizeGameSessionResult { SessionId = command.SessionId };

        var session = await _sessionRepository.GetByIdAsync(command.SessionId);
        if (session == null)
        {
            _logger.LogWarning("Session not found for finalize: {SessionId}", command.SessionId);
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
            var profile = await _profileRepository.GetByIdAsync(profileId);
            if (profile == null)
            {
                _logger.LogWarning("Profile {ProfileId} not found while finalizing session {SessionId}", profileId, session.Id);
                continue;
            }

            // Score first-time plays only (service skips if already scored)
            PlayerScenarioScore? score = await _scoringService.ScoreSessionAsync(session, profile);
            var alreadyPlayed = score == null;

            // Compute cumulative axis totals across all scored scenarios for this profile
            var allScores = await _scoreRepository.GetByProfileIdAsync(profile.Id);
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
            var newBadges = await _badgeService.AwardBadgesAsync(profile, cumulative);

            // Always include an entry so the client can show players who did/didn't receive a badge
            result.Awards.Add(new ProfileBadgeAwards
            {
                ProfileId = profile.Id,
                ProfileName = profile.Name,
                NewBadges = newBadges,
                AlreadyPlayed = alreadyPlayed
            });
        }

        _logger.LogInformation("Finalized session {SessionId}. New badge awards for {Count} profile(s).",
            session.Id, result.Awards.Count);

        return result;
    }
}
