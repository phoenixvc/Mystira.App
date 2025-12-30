using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.Domain.Models;
using ContractCharacterAssignmentDto = Mystira.Contracts.App.Models.CharacterAssignmentDto;

namespace Mystira.App.Application.CQRS.GameSessions.Commands;

/// <summary>
/// Wolverine handler for StartGameSessionCommand.
/// Creates a new game session with initial state.
/// Ensures only one active session exists per account/profile/scenario.
/// Uses static method convention for cleaner, more testable code.
/// </summary>
public static class StartGameSessionCommandHandler
{
    /// <summary>
    /// Handles the StartGameSessionCommand.
    /// Wolverine injects dependencies as method parameters.
    /// </summary>
    public static async Task<GameSession> Handle(
        StartGameSessionCommand command,
        IGameSessionRepository repository,
        IScenarioRepository scenarioRepository,
        IUnitOfWork unitOfWork,
        ILogger logger,
        CancellationToken ct)
    {
        var request = command.Request;

        if (string.IsNullOrEmpty(request.ScenarioId))
        {
            throw new ArgumentException("ScenarioId is required");
        }

        if (string.IsNullOrEmpty(request.AccountId))
        {
            throw new ArgumentException("AccountId is required");
        }

        if (string.IsNullOrEmpty(request.ProfileId))
        {
            throw new ArgumentException("ProfileId is required");
        }

        if ((request.PlayerNames == null || !request.PlayerNames.Any())
            && (request.CharacterAssignments == null || !request.CharacterAssignments.Any()))
        {
            throw new ArgumentException("At least one player or character assignment is required");
        }

        // If there is already an active session for this scenario/account/profile, reuse it.
        // This prevents duplicates caused by retries, double-clicks, or user re-entering start flow.
        var existingActiveSessions = (await repository
                .GetActiveSessionsByScenarioAndAccountAsync(request.ScenarioId, request.AccountId))
            .Where(s => string.Equals(s.ProfileId, request.ProfileId, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(s => s.StartTime)
            .ToList();

        if (existingActiveSessions.Any())
        {
            var primary = existingActiveSessions.First();
            var duplicates = existingActiveSessions.Skip(1).ToList();

            if (duplicates.Any())
            {
                logger.LogWarning(
                    "Found {Count} duplicate active sessions for ScenarioId={ScenarioId}, AccountId={AccountId}, ProfileId={ProfileId}. Cleaning up.",
                    duplicates.Count,
                    request.ScenarioId,
                    request.AccountId,
                    request.ProfileId);

                foreach (var duplicate in duplicates)
                {
                    if (IsEffectivelyEmptySession(duplicate))
                    {
                        await repository.DeleteAsync(duplicate.Id);
                    }
                    else
                    {
                        duplicate.Status = SessionStatus.Abandoned;
                        duplicate.EndTime = DateTime.UtcNow;
                        duplicate.ElapsedTime = duplicate.EndTime.Value - duplicate.StartTime;
                        await repository.UpdateAsync(duplicate);
                    }
                }

                await unitOfWork.SaveChangesAsync(ct);
            }

            var updated = false;

            // Hydrate missing assignments/names if the existing session was created earlier without them.
            if ((primary.CharacterAssignments == null || primary.CharacterAssignments.Count == 0)
                && request.CharacterAssignments != null
                && request.CharacterAssignments.Any())
            {
                primary.CharacterAssignments = request.CharacterAssignments.Select(ca => MapToDomain(ca)).ToList();
                updated = true;
            }

            if ((primary.PlayerNames == null || primary.PlayerNames.Count == 0)
                && request.PlayerNames != null
                && request.PlayerNames.Any())
            {
                primary.PlayerNames = request.PlayerNames.ToList();
                updated = true;
            }

            if ((primary.PlayerNames == null || primary.PlayerNames.Count == 0)
                && primary.CharacterAssignments != null
                && primary.CharacterAssignments.Any())
            {
                primary.PlayerNames = primary.CharacterAssignments
                    .Where(ca => !ca.IsUnused && ca.PlayerAssignment != null)
                    .Select(ca => ca.PlayerAssignment!.ProfileName ?? ca.PlayerAssignment!.GuestName ?? "Player")
                    .ToList();
                updated = true;
            }

            // Ensure the server-side session has an initial CurrentSceneId so it can be resumed cleanly.
            if (string.IsNullOrWhiteSpace(primary.CurrentSceneId))
            {
                var scenario = await scenarioRepository.GetByIdAsync(request.ScenarioId);
                if (scenario != null)
                {
                    primary.CurrentSceneId = DetermineStartingSceneId(scenario);
                    primary.SceneCount = scenario.Scenes?.Count ?? primary.SceneCount;
                    updated = true;
                }
            }

            if (updated)
            {
                await repository.UpdateAsync(primary);
                await unitOfWork.SaveChangesAsync(ct);
            }

            logger.LogInformation(
                "Reusing existing active game session {SessionId} for ScenarioId={ScenarioId}, AccountId={AccountId}, ProfileId={ProfileId}",
                primary.Id,
                request.ScenarioId,
                request.AccountId,
                request.ProfileId);

            return primary;
        }

        var scenarioEntity = await scenarioRepository.GetByIdAsync(request.ScenarioId);
        if (scenarioEntity == null)
        {
            // Be tolerant in case a stale/invalid scenario id is provided (or tests that don't seed scenarios).
            // When scenario data is unavailable, we still create the session, but cannot initialize scene/axis metadata.
            logger.LogWarning(
                "Scenario {ScenarioId} was not found when starting a game session. Creating session without scenario metadata.",
                request.ScenarioId);
        }

        var targetAgeGroup = AgeGroup.Parse(request.TargetAgeGroup) ?? new AgeGroup(6, 9);
        if (scenarioEntity != null && scenarioEntity.MinimumAge > targetAgeGroup.MinimumAge)
        {
            throw new ArgumentException(
                $"Scenario minimum age ({scenarioEntity.MinimumAge}) exceeds target age group ({request.TargetAgeGroup})");
        }

        var session = new GameSession
        {
            Id = Guid.NewGuid().ToString("N"),
            ScenarioId = request.ScenarioId,
            AccountId = request.AccountId,
            ProfileId = request.ProfileId,
            PlayerNames = request.PlayerNames ?? new List<string>(),
            TargetAgeGroup = targetAgeGroup,
            Status = SessionStatus.InProgress,
            StartTime = DateTime.UtcNow,
            CurrentSceneId = scenarioEntity == null ? string.Empty : DetermineStartingSceneId(scenarioEntity),
            ChoiceHistory = new List<SessionChoice>(),
            EchoHistory = new List<EchoLog>(),
            Achievements = new List<SessionAchievement>(),
            CompassValues = new Dictionary<string, CompassTracking>(StringComparer.OrdinalIgnoreCase),
            SceneCount = scenarioEntity?.Scenes?.Count ?? 0
        };

        if (request.CharacterAssignments != null && request.CharacterAssignments.Any())
        {
            session.CharacterAssignments = request.CharacterAssignments.Select(ca => MapToDomain(ca)).ToList();

            // If PlayerNames not provided, derive from assignments (non-unused only)
            if (session.PlayerNames == null || !session.PlayerNames.Any())
            {
                session.PlayerNames = session.CharacterAssignments
                    .Where(ca => !ca.IsUnused && ca.PlayerAssignment != null)
                    .Select(ca => ca.PlayerAssignment!.ProfileName ?? ca.PlayerAssignment!.GuestName ?? "Player")
                    .ToList();
            }
        }

        // Initialize compass tracking for scenario axes
        if (scenarioEntity?.CoreAxes != null)
        {
            foreach (var axis in scenarioEntity.CoreAxes)
            {
                session.CompassValues[axis.Value] = new CompassTracking
                {
                    Axis = axis.Value,
                    CurrentValue = 0.0,
                    StartingValue = 0.0,
                    History = new List<CompassChange>(),
                    LastUpdated = DateTime.UtcNow
                };
            }
        }

        await repository.AddAsync(session);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Started game session {SessionId} for scenario {ScenarioId}, account {AccountId}",
            session.Id,
            request.ScenarioId,
            request.AccountId);

        return session;
    }

    private static bool IsEffectivelyEmptySession(GameSession session)
    {
        var hasChoices = session.ChoiceHistory?.Count > 0;
        var hasEchoes = session.EchoHistory?.Count > 0;
        var hasAchievements = session.Achievements?.Count > 0;
        var hasScene = !string.IsNullOrWhiteSpace(session.CurrentSceneId);

        return !hasChoices && !hasEchoes && !hasAchievements && !hasScene;
    }

    private static string DetermineStartingSceneId(Scenario scenario)
    {
        if (scenario.Scenes == null || scenario.Scenes.Count == 0)
        {
            return string.Empty;
        }

        var referenced = scenario.Scenes
            .Where(s => !string.IsNullOrWhiteSpace(s.NextSceneId))
            .Select(s => s.NextSceneId!)
            .Concat(scenario.Scenes
                .SelectMany(s => s.Branches ?? new List<Branch>())
                .Where(b => !string.IsNullOrWhiteSpace(b.NextSceneId))
                .Select(b => b.NextSceneId))
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return scenario.Scenes.FirstOrDefault(s => !referenced.Contains(s.Id))?.Id
            ?? scenario.Scenes.First().Id;
    }

    private static SessionCharacterAssignment MapToDomain(ContractCharacterAssignmentDto dto)
    {
        return new SessionCharacterAssignment
        {
            CharacterId = dto.CharacterId,
            CharacterName = dto.CharacterName,
            Image = dto.Image,
            Audio = dto.Audio,
            Role = dto.Role,
            Archetype = dto.Archetype,
            PlayerAssignment = dto.PlayerAssignment == null
                ? null
                : new SessionPlayerAssignment
                {
                    Type = dto.PlayerAssignment.Type,
                    ProfileId = dto.PlayerAssignment.ProfileId,
                    ProfileName = dto.PlayerAssignment.ProfileName,
                    ProfileImage = dto.PlayerAssignment.ProfileImage,
                    SelectedAvatarMediaId = dto.PlayerAssignment.SelectedAvatarMediaId,
                    GuestName = dto.PlayerAssignment.GuestName,
                    GuestAgeRange = dto.PlayerAssignment.GuestAgeRange,
                    GuestAvatar = dto.PlayerAssignment.GuestAvatar,
                    SaveAsProfile = dto.PlayerAssignment.SaveAsProfile
                }
        };
    }
}
