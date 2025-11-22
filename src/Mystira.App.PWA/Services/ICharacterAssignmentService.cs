using Mystira.App.PWA.Models;

namespace Mystira.App.PWA.Services;

public interface ICharacterAssignmentService
{
    Task<CharacterAssignmentResponse> GetCharacterAssignmentDataAsync(string scenarioId,
        List<CharacterAssignment> existingAssignments);
    Task<bool> StartGameSessionWithAssignmentsAsync(StartGameSessionRequest request);
    Task<UserProfile?> CreateGuestProfileAsync(CreateGuestProfileRequest request);
    Task<List<UserProfile>?> GetAvailableProfilesAsync();
    Task<Character?> GetCharacterDetailsAsync(string characterId);
}
