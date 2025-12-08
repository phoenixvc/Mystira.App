using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;
using Mystira.App.Contracts.Models.GameSessions;

namespace Mystira.App.Application.CQRS.GameSessions.Commands;

/// <summary>
/// Handler for StartGameSessionCommand
/// Creates a new game session with initial state
/// </summary>
public class StartGameSessionCommandHandler : ICommandHandler<StartGameSessionCommand, GameSession>
{
    private readonly IGameSessionRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<StartGameSessionCommandHandler> _logger;

    public StartGameSessionCommandHandler(
        IGameSessionRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<StartGameSessionCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<GameSession> Handle(
        StartGameSessionCommand command,
        CancellationToken cancellationToken)
    {
        var request = command.Request;

        // Validate request
        if (string.IsNullOrEmpty(request.ScenarioId))
            throw new ArgumentException("ScenarioId is required");
        if (string.IsNullOrEmpty(request.AccountId))
            throw new ArgumentException("AccountId is required");
        if (string.IsNullOrEmpty(request.ProfileId))
            throw new ArgumentException("ProfileId is required");
        if ((request.PlayerNames == null || !request.PlayerNames.Any())
            && (request.CharacterAssignments == null || !request.CharacterAssignments.Any()))
            throw new ArgumentException("At least one player or character assignment is required");

        // Create new game session
        var session = new GameSession
        {
            Id = Guid.NewGuid().ToString("N"),
            ScenarioId = request.ScenarioId,
            AccountId = request.AccountId,
            ProfileId = request.ProfileId,
            PlayerNames = request.PlayerNames ?? new List<string>(),
            TargetAgeGroup = AgeGroup.Parse(request.TargetAgeGroup) ?? new AgeGroup(6, 9),
            Status = SessionStatus.InProgress,
            StartTime = DateTime.UtcNow,
            ChoiceHistory = new List<SessionChoice>(),
            EchoHistory = new List<EchoLog>(),
            Achievements = new List<SessionAchievement>(),
            CompassValues = new Dictionary<string, CompassTracking>()
        };

        // Map character assignments if provided
        if (request.CharacterAssignments != null && request.CharacterAssignments.Any())
        {
            session.CharacterAssignments = request.CharacterAssignments.Select(MapToDomain).ToList();

            // If PlayerNames not provided, derive from assignments (non-unused only)
            if (session.PlayerNames == null || !session.PlayerNames.Any())
            {
                session.PlayerNames = session.CharacterAssignments
                    .Where(ca => !ca.IsUnused && ca.PlayerAssignment != null)
                    .Select(ca => ca.PlayerAssignment!.ProfileName ?? ca.PlayerAssignment!.GuestName ?? "Player")
                    .ToList();
            }
        }

        // Add to repository
        await _repository.AddAsync(session);

        // Persist changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Started game session {SessionId} for scenario {ScenarioId}, account {AccountId}",
            session.Id, request.ScenarioId, request.AccountId);

        return session;
    }

    private static SessionCharacterAssignment MapToDomain(CharacterAssignmentDto dto)
    {
        return new SessionCharacterAssignment
        {
            CharacterId = dto.CharacterId,
            CharacterName = dto.CharacterName,
            Image = dto.Image,
            Audio = dto.Audio,
            Role = dto.Role,
            Archetype = dto.Archetype,
            IsUnused = dto.IsUnused,
            PlayerAssignment = dto.PlayerAssignment == null ? null : new SessionPlayerAssignment
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
