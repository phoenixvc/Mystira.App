using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;

namespace Mystira.App.Application.CQRS.UserProfiles.Commands;

/// <summary>
/// Handler for assigning a character to a user profile.
/// Validates that both the profile and character exist before assignment.
/// </summary>
public class AssignCharacterToProfileCommandHandler : ICommandHandler<AssignCharacterToProfileCommand, bool>
{
    private readonly IUserProfileRepository _profileRepository;
    private readonly ICharacterMapRepository _characterRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AssignCharacterToProfileCommandHandler> _logger;

    public AssignCharacterToProfileCommandHandler(
        IUserProfileRepository profileRepository,
        ICharacterMapRepository characterRepository,
        IUnitOfWork unitOfWork,
        ILogger<AssignCharacterToProfileCommandHandler> logger)
    {
        _profileRepository = profileRepository;
        _characterRepository = characterRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> Handle(
        AssignCharacterToProfileCommand command,
        CancellationToken cancellationToken)
    {
        // Validate profile exists
        var profile = await _profileRepository.GetByIdAsync(command.ProfileId);
        if (profile == null)
        {
            _logger.LogWarning("Cannot assign character: Profile not found {ProfileId}", command.ProfileId);
            return false;
        }

        // Validate character exists
        var character = await _characterRepository.GetByIdAsync(command.CharacterId);
        if (character == null)
        {
            _logger.LogWarning("Cannot assign character: Character not found {CharacterId}", command.CharacterId);
            return false;
        }

        // Update profile with character assignment
        profile.IsNpc = command.IsNpc;
        profile.UpdatedAt = DateTime.UtcNow;

        await _profileRepository.UpdateAsync(profile);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Assigned character {CharacterId} to profile {ProfileId} (NPC: {IsNpc})",
            command.CharacterId, command.ProfileId, command.IsNpc);

        return true;
    }
}
