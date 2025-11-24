using Microsoft.Extensions.Logging;
using Mystira.App.Application.Interfaces;
using Mystira.App.Application.Ports.Data;

namespace Mystira.App.Application.CQRS.UserProfiles.Commands;

/// <summary>
/// Handler for DeleteUserProfileCommand
/// Deletes a user profile and all associated data (COPPA compliance)
/// </summary>
public class DeleteUserProfileCommandHandler : ICommandHandler<DeleteUserProfileCommand, bool>
{
    private readonly IUserProfileRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteUserProfileCommandHandler> _logger;

    public DeleteUserProfileCommandHandler(
        IUserProfileRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<DeleteUserProfileCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> Handle(
        DeleteUserProfileCommand command,
        CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByIdAsync(command.ProfileId);
        if (profile == null)
        {
            _logger.LogWarning("Profile not found: {ProfileId}", command.ProfileId);
            return false;
        }

        // Delete profile
        await _repository.DeleteAsync(profile.Id);

        // Persist changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted user profile {ProfileId}", command.ProfileId);

        return true;
    }
}
