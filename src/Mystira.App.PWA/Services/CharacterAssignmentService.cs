using Mystira.App.PWA.Models;

namespace Mystira.App.PWA.Services;

public class CharacterAssignmentService : ICharacterAssignmentService
{
    private readonly ILogger<CharacterAssignmentService> _logger;
    private readonly IApiClient _apiClient;
    private readonly IAuthService _authService;
    private readonly IGameSessionService _gameSessionService;

    public CharacterAssignmentService(
        ILogger<CharacterAssignmentService> logger,
        IApiClient apiClient,
        IAuthService authService,
        IGameSessionService gameSessionService)
    {
        _logger = logger;
        _apiClient = apiClient;
        _authService = authService;
        _gameSessionService = gameSessionService;
    }

    public async Task<CharacterAssignmentResponse> GetCharacterAssignmentDataAsync(string scenarioId,
        List<CharacterAssignment> existingAssignments)
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

            // Update character assignments with existing data
            foreach (var assignment in existingAssignments)
            {
                if (assignment.PlayerAssignment == null)
                {
                    continue; // Skip unused assignments
                }

                var existingAssignment = characterAssignments.FirstOrDefault(a => a.CharacterId == assignment.CharacterId);
                if (existingAssignment == null)
                {
                    continue;
                }

                existingAssignment.PlayerAssignment = assignment.PlayerAssignment;
                existingAssignment.IsUnused = assignment.IsUnused;
            }

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

            // Call backend API that supports character assignments
            var apiGameSession = await _apiClient.StartGameSessionWithAssignmentsAsync(request);

            if (apiGameSession == null)
            {
                _logger.LogWarning("Failed to start game session for scenario: {ScenarioId}", request.ScenarioId);
                return false;
            }

            // Populate the local game session so GameSessionPage can access it
            if (request.Scenario != null)
            {
                _logger.LogInformation("Populating local game session with scenario data");

                // Find the starting scene - look for a scene that's not referenced by any other scene
                var scenario = request.Scenario;
                var allReferencedSceneIds = scenario.Scenes
                    .Where(s => !string.IsNullOrEmpty(s.NextSceneId))
                    .Select(s => s.NextSceneId)
                    .Concat(scenario.Scenes
                        .SelectMany(s => s.Branches)
                        .Where(b => !string.IsNullOrEmpty(b.NextSceneId))
                        .Select(b => b.NextSceneId))
                    .Where(id => !string.IsNullOrEmpty(id))
                    .ToHashSet();

                var startingScene = scenario.Scenes.FirstOrDefault(s => !allReferencedSceneIds.Contains(s.Id));

                if (startingScene == null)
                {
                    // Fallback to first scene if we can't determine the starting scene
                    startingScene = scenario.Scenes.FirstOrDefault();
                }

                if (startingScene != null)
                {
                    // Resolve media URLs for the starting scene
                    startingScene.AudioUrl = !string.IsNullOrEmpty(startingScene.Media?.Audio)
                        ? await _apiClient.GetMediaUrlFromId(startingScene.Media.Audio)
                        : null;
                    startingScene.ImageUrl = !string.IsNullOrEmpty(startingScene.Media?.Image)
                        ? await _apiClient.GetMediaUrlFromId(startingScene.Media.Image)
                        : null;
                    startingScene.VideoUrl = !string.IsNullOrEmpty(startingScene.Media?.Video)
                        ? await _apiClient.GetMediaUrlFromId(startingScene.Media.Video)
                        : null;

                    // Create local game session
                    var localGameSession = new GameSession
                    {
                        Id = apiGameSession.Id,
                        Scenario = scenario,
                        ScenarioId = scenario.Id,
                        ScenarioName = scenario.Title,
                        CurrentScene = startingScene,
                        StartedAt = apiGameSession.StartedAt,
                        CompletedScenes = new List<Scene>(),
                        IsCompleted = false,
                        CharacterAssignments = apiGameSession.CharacterAssignments?.Any() == true
                            ? new List<CharacterAssignment>(apiGameSession.CharacterAssignments)
                            : (request.CharacterAssignments?.Any() == true
                                ? new List<CharacterAssignment>(request.CharacterAssignments)
                                : new List<CharacterAssignment>())
                    };

                    // Set the current game session in GameSessionService
                    _gameSessionService.SetCurrentGameSession(localGameSession);

                    // Also store assignments in the session service for placeholder replacement
                    _gameSessionService.SetCharacterAssignments(localGameSession.CharacterAssignments);

                    _logger.LogInformation("Local game session populated with starting scene: {SceneTitle}", startingScene.Title);
                }
                else
                {
                    _logger.LogWarning("No starting scene found for scenario: {ScenarioId}", request.ScenarioId);
                }
            }
            else
            {
                _logger.LogWarning("No scenario provided in request, local game session will not be populated");
            }

            // Set character assignments in the session service for scenarios where local population didn't occur
            if ((request.Scenario == null))
            {
                var assignments = apiGameSession.CharacterAssignments?.Any() == true
                    ? apiGameSession.CharacterAssignments
                    : request.CharacterAssignments;
                if (assignments?.Any() == true)
                {
                    _gameSessionService.SetCharacterAssignments(assignments);
                }
            }

            _logger.LogInformation("Game session started successfully with ID: {SessionId}", apiGameSession.Id);
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
                AgeGroup = request.AgeRange ?? "1-2",
                HasCompletedOnboarding = request.IsGuest // Guest profiles skip onboarding
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

    private Task<List<CharacterAssignment>> CreateCharacterAssignmentsAsync(Scenario scenario)
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

        return Task.FromResult(assignments);
    }
}
