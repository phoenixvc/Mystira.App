using Microsoft.Extensions.Logging;
using Mystira.App.Domain.Models;
using Mystira.App.Infrastructure.Data.Repositories;
using Mystira.App.Infrastructure.Data.UnitOfWork;

namespace Mystira.App.Application.UseCases.UserProfiles;

/// <summary>
/// Use case for deleting a user profile and associated data (COPPA compliance)
/// </summary>
public class DeleteUserProfileUseCase
{
    private readonly IUserProfileRepository _repository;
    private readonly IGameSessionRepository _gameSessionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteUserProfileUseCase> _logger;

    public DeleteUserProfileUseCase(
        IUserProfileRepository repository,
        IGameSessionRepository gameSessionRepository,
        IUnitOfWork unitOfWork,
        ILogger<DeleteUserProfileUseCase> logger)
    {
        _repository = repository;
        _gameSessionRepository = gameSessionRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> ExecuteAsync(string id)
    {
        var profile = await _repository.GetByIdAsync(id);
        if (profile == null)
        {
            return false;
        }

        // COPPA compliance: Also delete associated sessions, badges, and data
        var sessions = await _gameSessionRepository.GetByProfileIdAsync(profile.Id);
        foreach (var session in sessions)
        {
            await _gameSessionRepository.DeleteAsync(session.Id);
        }

        await _repository.DeleteAsync(profile.Id);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Deleted user profile and associated data: {ProfileId} - {Name}", profile.Id, profile.Name);
        return true;
    }
}

