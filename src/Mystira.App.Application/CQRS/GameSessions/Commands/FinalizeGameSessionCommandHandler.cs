using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Application.Services;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.GameSessions.Commands;

public class FinalizeGameSessionCommandHandler : ICommandHandler<FinalizeGameSessionCommand, FinalizeGameSessionResult>
{
    private readonly IGameSessionRepository _sessionRepository;
    private readonly IUserProfileRepository _profileRepository;
    private readonly IAxisScoringService _scoringService;
    private readonly IBadgeAwardingService _badgeService;
    private readonly ILogger<FinalizeGameSessionCommandHandler> _logger;

    public FinalizeGameSessionCommandHandler(
        IGameSessionRepository sessionRepository,
        IUserProfileRepository profileRepository,
        IAxisScoringService scoringService,
        IBadgeAwardingService badgeService,
        ILogger<FinalizeGameSessionCommandHandler> logger)
    {
        _sessionRepository = sessionRepository;
        _profileRepository = profileRepository;
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

        // Determine participating profiles (replicate logic from GameSession private method)
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
            if (score == null)
            {
                continue; // replay for this profile+scenario
            }

            // Award badges based on aggregated axis scores from this session
            var newBadges = await _badgeService.AwardBadgesAsync(profile, score.AxisScores);
            if (newBadges.Count > 0)
            {
                result.Awards.Add(new ProfileBadgeAwards
                {
                    ProfileId = profile.Id,
                    ProfileName = profile.Name,
                    NewBadges = newBadges
                });
            }
        }

        _logger.LogInformation("Finalized session {SessionId}. New badge awards for {Count} profile(s).",
            session.Id, result.Awards.Count);

        return result;
    }
}
