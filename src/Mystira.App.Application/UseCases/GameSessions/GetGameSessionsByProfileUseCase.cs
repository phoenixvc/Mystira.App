using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.Domain.Models;

namespace Mystira.App.Application.UseCases.GameSessions;

/// <summary>
/// Use case for retrieving game sessions by profile ID
/// </summary>
public class GetGameSessionsByProfileUseCase
{
    private readonly IGameSessionRepository _repository;
    private readonly ILogger<GetGameSessionsByProfileUseCase> _logger;

    public GetGameSessionsByProfileUseCase(
        IGameSessionRepository repository,
        ILogger<GetGameSessionsByProfileUseCase> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<List<GameSession>> ExecuteAsync(string profileId)
    {
        if (string.IsNullOrWhiteSpace(profileId))
        {
            throw new ArgumentException("Profile ID cannot be null or empty", nameof(profileId));
        }

        var sessions = await _repository.GetByProfileIdAsync(profileId);
        var sessionList = sessions.ToList();

        _logger.LogInformation("Retrieved {Count} game sessions for profile {ProfileId}", sessionList.Count, profileId);

        return sessionList;
    }
}

