using System.Linq;
using Mystira.App.PWA.Models;

namespace Mystira.App.PWA.Services;

public class CharacterAssignmentService : ICharacterAssignmentService
{
    private readonly ILogger<CharacterAssignmentService> _logger;
    private readonly IApiClient _apiClient;
    private readonly IAuthService _authService;

    public CharacterAssignmentService(
        ILogger<CharacterAssignmentService> logger,
        IApiClient apiClient,
        IAuthService authService)
    {
        _logger = logger;
        _apiClient = apiClient;
        _authService = authService;
    }

    public async Task<CharacterAssignmentResponse> GetCharacterAssignmentDataAsync(string scenarioId)
    {
        try
        {
            _logger.LogInformation("Getting character assignment data for scenario: {ScenarioId}", scenarioId);

            // Get scenario details
            var scenario = await _apiClient.GetScenarioAsync(scenarioId);
            if (scenario == null)
            {
                _logger.LogError("Scenario not found: {ScenarioId}", scenarioId);
                return new CharacterAssignmentResponse();
            }

            // Get available profiles for the current account
            var availableProfiles = await GetAvailableProfilesAsync();

            // Create character assignments (always 4 slots)
            var characterAssignments = await CreateCharacterAssignmentsAsync(scenario);

            _logger.LogInformation("Created {Count} character assignments for scenario: {ScenarioId}", 
                characterAssignments.Count, scenarioId);

            return new CharacterAssignmentResponse
            {
                Scenario = scenario,
                CharacterAssignments = characterAssignments,
                AvailableProfiles = availableProfiles ?? new List<UserProfile>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting character assignment data for scenario: {ScenarioId}", scenarioId);
            return new CharacterAssignmentResponse();
        }
    }

    public async Task<bool> StartGameSessionWithAssignmentsAsync(StartGameSessionRequest request)
    {
        try
        {
            _logger.LogInformation("Starting game session with {Count} character assignments for scenario: {ScenarioId}", 
                request.CharacterAssignments.Count, request.ScenarioId);

            // Convert character assignments to player names list for backward compatibility
            var playerNames = request.CharacterAssignments
                .Where(ca => ca.PlayerAssignment != null && !ca.IsUnused)
                .Select(ca => ca.PlayerAssignment!.ProfileName ?? ca.PlayerAssignment!.GuestName ?? "Unknown Player")
                .ToList();

            // Use the existing API client method for now - this will need to be updated on the backend
            // to support character assignments
            var gameSession = await _apiClient.StartGameSessionAsync(
                request.ScenarioId,
                request.AccountId,
                request.ProfileId,
                playerNames,
                request.TargetAgeGroup);

            if (gameSession == null)
            {
                _logger.LogWarning("Failed to start game session for scenario: {ScenarioId}", request.ScenarioId);
                return false;
            }

            // TODO: Save character assignments to the game session
            // This would require a new API endpoint to update the game session with character assignments

            _logger.LogInformation("Game session started successfully with ID: {SessionId}", gameSession.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting game session with character assignments for scenario: {ScenarioId}", 
                request.ScenarioId);
            return false;
        }
    }

    public async Task<UserProfile?> CreateGuestProfileAsync(CreateGuestProfileRequest request)
    {
        try
        {
            _logger.LogInformation("Creating guest profile: {Name}", request.Name);

            var createRequest = new CreateUserProfileRequest
            {
                Name = request.Name,
                IsGuest = request.IsGuest,
                AccountId = request.AccountId,
                AgeGroupName = GetAgeGroupNameFromRange(request.AgeRange),
                HasCompletedOnboarding = true // Guest profiles skip onboarding
            };

            var profile = await _apiClient.CreateProfileAsync(createRequest);
            
            if (profile != null)
            {
                _logger.LogInformation("Successfully created guest profile: {Name} with ID: {Id}", request.Name, profile.Id);
            }

            return profile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating guest profile: {Name}", request.Name);
            return null;
        }
    }

    public async Task<List<UserProfile>?> GetAvailableProfilesAsync()
    {
        try
        {
            _logger.LogInformation("Getting available profiles for current account");

            var account = await _authService.GetCurrentAccountAsync();
            if (account == null)
            {
                _logger.LogWarning("No account found for getting profiles");
                return new List<UserProfile>();
            }

            var profiles = await _apiClient.GetProfilesByAccountAsync(account.Id);
            
            if (profiles != null)
            {
                _logger.LogInformation("Found {Count} profiles for account: {AccountId}", profiles.Count, account.Id);
            }

            return profiles;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available profiles");
            return new List<UserProfile>();
        }
    }

    public async Task<Character?> GetCharacterDetailsAsync(string characterId)
    {
        try
        {
            _logger.LogInformation("Getting character details: {CharacterId}", characterId);
            
            var character = await _apiClient.GetCharacterAsync(characterId);
            
            if (character != null)
            {
                _logger.LogInformation("Successfully retrieved character: {CharacterName}", character.Name);
            }

            return character;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting character details: {CharacterId}", characterId);
            return null;
        }
    }

    private async Task<List<CharacterAssignment>> CreateCharacterAssignmentsAsync(Scenario scenario)
    {
        var assignments = new List<CharacterAssignment>();

        // Create assignments for scenario characters (up to 4)
        var characterCount = scenario.Characters?.Count ?? 0;
        for (int i = 0; i < Math.Min(characterCount, 4); i++)
        {
            var scenarioChar = scenario.Characters![i];
            var assignment = new CharacterAssignment
            {
                CharacterId = scenarioChar.Id,
                CharacterName = scenarioChar.Name,
                Image = scenarioChar.Image,
                Audio = scenarioChar.Audio,
                Role = scenarioChar.Metadata?.Role?.FirstOrDefault() ?? "",
                Archetype = scenarioChar.Metadata?.Archetype?.FirstOrDefault() ?? "",
                IsUnused = false
            };

            // Get full character details if available
            if (!string.IsNullOrEmpty(assignment.CharacterId))
            {
                var fullCharacter = await GetCharacterDetailsAsync(assignment.CharacterId);
                if (fullCharacter != null)
                {
                    assignment.Image = fullCharacter.Image;
                    assignment.Audio = fullCharacter.Audio;
                    assignment.Role = fullCharacter.Role ?? assignment.Role;
                    assignment.Archetype = fullCharacter.Archetype ?? assignment.Archetype;
                }
            }

            assignments.Add(assignment);
        }

        // Fill remaining slots with "Unused" characters
        for (int i = assignments.Count; i < 4; i++)
        {
            assignments.Add(new CharacterAssignment
            {
                CharacterId = $"unused-{i}",
                CharacterName = "Unused Character",
                Role = "Empty Slot",
                Archetype = "No Assignment",
                IsUnused = true
            });
        }

        return assignments;
    }

    private string GetAgeGroupNameFromRange(string? ageRange)
    {
        return ageRange switch
        {
            "1-2" => "toddlers",
            "3-5" => "preschoolers",
            "6-9" => "school",
            "10-12" => "preteens",
            "13-18" => "teens",
            _ => "school"
        };
    }
}
