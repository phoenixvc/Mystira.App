using Mystira.App.PWA.Models;

namespace Mystira.App.PWA.Services;

public class GameSessionService : IGameSessionService
{
    private readonly ILogger<GameSessionService> _logger;
    private readonly IApiClient _apiClient;
    private readonly IAuthService _authService;

    public event EventHandler<GameSession?>? GameSessionChanged;

    private GameSession? _currentGameSession;
    public GameSession? CurrentGameSession
    {
        get => _currentGameSession;
        private set
        {
            _currentGameSession = value;
            GameSessionChanged?.Invoke(this, value);
        }
    }

    // Store character assignments for text replacement
    private List<CharacterAssignment> _characterAssignments = new();

    public GameSessionService(ILogger<GameSessionService> logger, IApiClient apiClient, IAuthService authService)
    {
        _logger = logger;
        _apiClient = apiClient;
        _authService = authService;
    }

    public async Task<bool> StartGameSessionAsync(Scenario scenario)
    {
        try
        {
            _logger.LogInformation("Starting game session for scenario: {ScenarioName}", scenario.Title);

            // Get account information from auth service
            var account = await _authService.GetCurrentAccountAsync();
            string accountId = account?.Id ?? "default-account";
            string profileId = "default-profile";

            // If account has profiles, use the first one as default
            if (account?.UserProfileIds != null && account.UserProfileIds.Any())
            {
                profileId = account.UserProfileIds.First();
            }

            _logger.LogInformation("Starting session with AccountId: {AccountId}, ProfileId: {ProfileId}", accountId, profileId);

            // Start session via API
            var apiGameSession = await _apiClient.StartGameSessionAsync(
                scenario.Id,
                accountId,
                profileId,
                new List<string> { "Player" }, // Default player name for now
                scenario.AgeGroup ?? "6-9" // Default age group
            );

            if (apiGameSession == null)
            {
                _logger.LogWarning("Failed to start game session via API for scenario: {ScenarioName}", scenario.Title);
                return false;
            }

            // Find the starting scene - look for a scene that's not referenced by any other scene
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

            if (startingScene == null)
            {
                _logger.LogError("No starting scene found for scenario: {ScenarioName}", scenario.Title);
                return false;
            }

            startingScene.AudioUrl = !string.IsNullOrEmpty(startingScene.Media?.Audio) ? await _apiClient.GetMediaUrlFromId(startingScene.Media.Audio) : null;
            startingScene.ImageUrl = !string.IsNullOrEmpty(startingScene.Media?.Image) ? await _apiClient.GetMediaUrlFromId(startingScene.Media.Image) : null;
            startingScene.VideoUrl = !string.IsNullOrEmpty(startingScene.Media?.Video) ? await _apiClient.GetMediaUrlFromId(startingScene.Media.Video) : null;

            // Create local game session with API session data
            CurrentGameSession = new GameSession
            {
                Id = apiGameSession.Id,
                Scenario = scenario,
                ScenarioId = scenario.Id,
                ScenarioName = scenario.Title,
                CurrentScene = startingScene,
                StartedAt = apiGameSession.StartedAt,
                CompletedScenes = new List<Scene>(),
                IsCompleted = false
            };

            // Set empty character assignments for scenarios that skip character assignment
            // This ensures text replacement functionality works (even though no replacements will occur)
            if (!_characterAssignments.Any())
            {
                _characterAssignments = new List<CharacterAssignment>();
            }

            _logger.LogInformation("Game session started successfully with ID: {SessionId}", apiGameSession.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting game session for scenario: {ScenarioName}", scenario.Title);
            return false;
        }
    }

    public async Task<bool> NavigateToSceneAsync(string sceneId)
    {
        try
        {
            if (CurrentGameSession == null)
            {
                _logger.LogWarning("Cannot navigate to scene - no active game session");
                return false;
            }

            _logger.LogInformation("Navigating to scene: {SceneId}", sceneId);

            // Add current scene to completed scenes if it exists
            if (CurrentGameSession.CurrentScene != null && CurrentGameSession.CompletedScenes.All(s => s.Id != CurrentGameSession.CurrentScene.Id))
            {
                CurrentGameSession.CompletedScenes.Add(CurrentGameSession.CurrentScene);
            }

            // Try to get the scene from API
            var scene = CurrentGameSession.Scenario.Scenes.Find(x => x.Id == sceneId);

            if (scene == null)
            {
                _logger.LogError("Scene not found: {SceneId}", sceneId);
                return false;
            }

            // Call API to progress the session to the new scene
            var updatedSession = await _apiClient.ProgressSessionSceneAsync(CurrentGameSession.Id, sceneId);
            if (updatedSession == null)
            {
                _logger.LogWarning("Failed to progress session via API for scene: {SceneId}, continuing with local state", sceneId);
            }

            // Resolve Media URLs
            scene.AudioUrl = !string.IsNullOrEmpty(scene.Media?.Audio) ? await _apiClient.GetMediaUrlFromId(scene.Media.Audio) : null;
            scene.ImageUrl = !string.IsNullOrEmpty(scene.Media?.Image) ? await _apiClient.GetMediaUrlFromId(scene.Media.Image) : null;
            scene.VideoUrl = !string.IsNullOrEmpty(scene.Media?.Video) ? await _apiClient.GetMediaUrlFromId(scene.Media.Video) : null;

            CurrentGameSession.CurrentScene = scene;
            CurrentGameSession.CurrentSceneId = sceneId;

            // Progress the session on the server
            var progressedSession = await _apiClient.ProgressSessionSceneAsync(CurrentGameSession.Id, sceneId);
            if (progressedSession == null)
            {
                _logger.LogWarning("Failed to progress session on server, but continuing locally for scene: {SceneId}", sceneId);
            }
            
            // Check if this is a final scene
            if (scene is { SceneType: SceneType.Special, NextSceneId: null })
            {
                CurrentGameSession.IsCompleted = true;
                _logger.LogInformation("Game session completed");
            }

            // Trigger the event to notify subscribers
            GameSessionChanged?.Invoke(this, CurrentGameSession);

            _logger.LogInformation("Successfully navigated to scene: {SceneId}", sceneId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error navigating to scene: {SceneId}", sceneId);
            return false;
        }
    }

    public async Task<bool> CompleteGameSessionAsync()
    {
        try
        {
            if (CurrentGameSession == null)
            {
                _logger.LogWarning("Cannot complete game session - no active session");
                return false;
            }

            _logger.LogInformation("Completing game session for scenario: {ScenarioName}", CurrentGameSession.ScenarioName);

            // Call the API to end the session
            var apiSession = await _apiClient.EndGameSessionAsync(CurrentGameSession.Id);
            if (apiSession == null)
            {
                _logger.LogWarning("Failed to end game session via API");
                // Still mark as completed locally for UI consistency
            }

            // Mark scenario as completed for the account
            var account = await _authService.GetCurrentAccountAsync();
            if (account != null && CurrentGameSession != null)
            {
                var success = await _apiClient.CompleteScenarioForAccountAsync(account.Id, CurrentGameSession.ScenarioId);
                if (success)
                {
                    _logger.LogInformation("Marked scenario {ScenarioId} as completed for account {AccountId}", 
                        CurrentGameSession.ScenarioId, account.Id);
                }
                else
                {
                    _logger.LogWarning("Failed to mark scenario as completed for account");
                }
            }

            if (CurrentGameSession != null)
            {
                CurrentGameSession.IsCompleted = true;

                // Trigger the event to notify subscribers
                GameSessionChanged?.Invoke(this, CurrentGameSession);
            }

            _logger.LogInformation("Game session completed successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing game session");
            return false;
        }
    }

    public async Task<bool> NavigateFromRollAsync(bool isSuccess)
    {
        try
        {
            if (CurrentGameSession?.CurrentScene == null)
            {
                _logger.LogWarning("Cannot navigate from roll - no active game session or current scene");
                return false;
            }

            var currentScene = CurrentGameSession.CurrentScene;

            if (currentScene.SceneType != SceneType.Roll)
            {
                _logger.LogWarning("Current scene is not a roll scene");
                return false;
            }

            // For roll scenes, use the branches collection to determine next scene
            // First branch = success path, second branch = failure path
            var branches = currentScene.Branches;

            if (branches == null || !branches.Any())
            {
                _logger.LogWarning("Roll scene has no branches defined");

                // Fallback to NextSceneId if available
                if (!string.IsNullOrEmpty(currentScene.NextSceneId))
                {
                    return await NavigateToSceneAsync(currentScene.NextSceneId);
                }

                _logger.LogInformation("No navigation path available for roll scene. Completing game session.");
                return await CompleteGameSessionAsync();
            }

            // Select the appropriate branch based on success/failure
            var selectedBranch = isSuccess
                ? branches.FirstOrDefault()                // First branch for success
                : branches.Skip(1).FirstOrDefault();       // Second branch for failure

            if (selectedBranch == null)
            {
                _logger.LogWarning("Could not find appropriate branch for roll outcome (Success: {IsSuccess})", isSuccess);
                return await CompleteGameSessionAsync();
            }

            var nextSceneId = selectedBranch.NextSceneId;

            if (string.IsNullOrEmpty(nextSceneId))
            {
                _logger.LogInformation("Selected branch has no next scene specified (Success: {IsSuccess}). Completing game session.", isSuccess);
                return await CompleteGameSessionAsync();
            }

            _logger.LogInformation("Navigating from roll scene. Success: {IsSuccess}, Branch choice: '{BranchChoice}', Next scene: {NextSceneId}",
                isSuccess, selectedBranch.Choice, nextSceneId);

            return await NavigateToSceneAsync(nextSceneId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error navigating from roll (Success: {IsSuccess})", isSuccess);
            return false;
        }
    }

    public async Task<bool> GoToNextSceneAsync()
    {
        try
        {
            if (CurrentGameSession?.CurrentScene == null)
            {
                _logger.LogWarning("Cannot go to next scene - no active game session or current scene");
                return false;
            }

            var currentScene = CurrentGameSession.CurrentScene;
            var nextSceneId = currentScene.NextSceneId;

            if (string.IsNullOrEmpty(nextSceneId))
            {
                _logger.LogInformation("No next scene available, completing game session");
                return await CompleteGameSessionAsync();
            }

            _logger.LogInformation("Advancing to next scene: {NextSceneId}", nextSceneId);

            return await NavigateToSceneAsync(nextSceneId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error advancing to next scene");
            return false;
        }
    }

    public void ClearGameSession()
    {
        _logger.LogInformation("Clearing game session");
        CurrentGameSession = null;
        _characterAssignments.Clear();
    }

    public void SetCurrentGameSession(GameSession? session)
    {
        _logger.LogInformation("Setting current game session: {SessionId}", session?.Id ?? "null");
        CurrentGameSession = session;
    }

    /// <summary>
    /// Sets character assignments for the current session (for text replacement)
    /// </summary>
    public void SetCharacterAssignments(List<CharacterAssignment> assignments)
    {
        _characterAssignments = assignments ?? new List<CharacterAssignment>();
        _logger.LogInformation("Set {Count} character assignments for text replacement", _characterAssignments.Count);
    }

    /// <summary>
    /// Replaces character placeholders [c:CharacterName] with player names in text
    /// </summary>
    public string ReplaceCharacterPlaceholders(string text)
    {
        if (string.IsNullOrEmpty(text) || !_characterAssignments.Any())
        {
            return text;
        }

        foreach (var assignment in _characterAssignments)
        {
            if (assignment.PlayerAssignment != null)
            {
                string playerName = assignment.PlayerAssignment.Type switch
                {
                    "Profile" => assignment.PlayerAssignment.ProfileName ?? "Player",
                    "Guest" => assignment.PlayerAssignment.GuestName ?? "Player",
                    _ => "Player"
                };

                string placeholder = $"[c:{assignment.CharacterName.ToLower()}]";
                text = text.Replace(placeholder, playerName);
            }
        }

        // Replace any remaining [c:...] patterns with "Player"
        var remainingPattern = System.Text.RegularExpressions.Regex.Matches(text, @"\[c:([^\]]+)\]");
        foreach (System.Text.RegularExpressions.Match match in remainingPattern)
        {
            text = text.Replace(match.Value, "Player");
        }

        return text;
    }
}
