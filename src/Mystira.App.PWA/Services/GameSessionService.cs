using System.Text.RegularExpressions;
using Mystira.App.PWA.Models;
using Mystira.App.PWA.Services.Music;

namespace Mystira.App.PWA.Services;

public partial class GameSessionService : IGameSessionService
{
    private readonly ILogger<GameSessionService> _logger;
    private readonly IApiClient _apiClient;
    private readonly IAuthService _authService;
    private readonly SceneAudioOrchestrator _audioOrchestrator;

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

    // Track pause state
    private bool _isPaused = false;
    public bool IsPaused => _isPaused;

    public GameSessionService(ILogger<GameSessionService> logger, IApiClient apiClient, IAuthService authService, SceneAudioOrchestrator audioOrchestrator)
    {
        _logger = logger;
        _apiClient = apiClient;
        _authService = authService;
        _audioOrchestrator = audioOrchestrator;
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
                AccountId = !string.IsNullOrWhiteSpace(apiGameSession.AccountId) ? apiGameSession.AccountId : accountId,
                ProfileId = !string.IsNullOrWhiteSpace(apiGameSession.ProfileId) ? apiGameSession.ProfileId : profileId,
                CurrentScene = startingScene,
                StartedAt = apiGameSession.StartedAt,
                CompletedScenes = new List<Scene>(),
                IsCompleted = false,
                CharacterAssignments = _characterAssignments?.Any() == true
                    ? new List<CharacterAssignment>(_characterAssignments)
                    : new List<CharacterAssignment>()
            };

            // Orchestrate initial scene audio
            await _audioOrchestrator.EnterSceneAsync(startingScene, scenario);

            // Set empty character assignments for scenarios that skip character assignment
            // This ensures text replacement functionality works (even though no replacements will occur)
            if (_characterAssignments?.Any() != true)
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

            // If the session is paused, attempt to resume on the server before navigating
            if (_isPaused)
            {
                _logger.LogInformation("Session is paused locally. Attempting to resume before navigating to scene: {SceneId}", sceneId);
                var resumed = await ResumeGameSessionAsync();
                if (!resumed)
                {
                    _logger.LogWarning("Failed to resume session before navigation. Proceeding may fail.");
                }
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

            // Orchestrate Audio
            await _audioOrchestrator.EnterSceneAsync(scene, CurrentGameSession.Scenario);

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

            // Do NOT finalize here. The UI layer (GameSessionPage) is responsible for
            // calling FinalizeGameSession and rendering the awards modal, to avoid
            // duplicate/early finalization calls that can lead to empty award payloads.

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

    public async Task<bool> PauseGameSessionAsync()
    {
        try
        {
            if (CurrentGameSession == null)
            {
                _logger.LogWarning("Cannot pause game session - no active session");
                return false;
            }

            if (_isPaused)
            {
                _logger.LogWarning("Game session is already paused");
                return false;
            }

            _logger.LogInformation("Pausing game session: {SessionId}", CurrentGameSession.Id);

            // Call the API to pause the session
            var apiSession = await _apiClient.PauseGameSessionAsync(CurrentGameSession.Id);
            if (apiSession == null)
            {
                _logger.LogWarning("Failed to pause game session via API, but updating local state");
            }

            _isPaused = true;

            // Trigger the event to notify subscribers
            GameSessionChanged?.Invoke(this, CurrentGameSession);

            _logger.LogInformation("Game session paused successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pausing game session");
            return false;
        }
    }

    public async Task<bool> ResumeGameSessionAsync()
    {
        try
        {
            if (CurrentGameSession == null)
            {
                _logger.LogWarning("Cannot resume game session - no active session");
                return false;
            }

            // Even if local flag isn't paused (e.g., after app reload), attempt resume on server
            if (!_isPaused)
            {
                _logger.LogInformation("Local session not marked paused; attempting server resume anyway for session: {SessionId}", CurrentGameSession.Id);
            }

            _logger.LogInformation("Resuming game session: {SessionId}", CurrentGameSession.Id);

            // Call the API to resume the session
            var apiSession = await _apiClient.ResumeGameSessionAsync(CurrentGameSession.Id);
            if (apiSession == null)
            {
                _logger.LogWarning("Failed to resume game session via API, but updating local state");
            }

            _isPaused = false;

            // If API returned session data, merge essential fields and hydrate assignments
            if (apiSession != null)
            {
                // Preserve local scenario/scenes, but update metadata
                CurrentGameSession.IsCompleted = apiSession.IsCompleted;
                CurrentGameSession.StartedAt = apiSession.StartedAt;

                if (apiSession.CharacterAssignments != null && apiSession.CharacterAssignments.Any())
                {
                    CurrentGameSession.CharacterAssignments = new List<CharacterAssignment>(apiSession.CharacterAssignments);
                    SetCharacterAssignments(CurrentGameSession.CharacterAssignments);
                    _logger.LogInformation("Hydrated {Count} character assignments from resumed session", CurrentGameSession.CharacterAssignments.Count);
                }
                else
                {
                    _logger.LogInformation("No character assignments returned from resume API; retaining existing assignments cache ({Count})",
                        CurrentGameSession.CharacterAssignments?.Count ?? 0);
                }
            }

            // Trigger the event to notify subscribers
            GameSessionChanged?.Invoke(this, CurrentGameSession);

            _logger.LogInformation("Game session resumed successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resuming game session");
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

            // Progression is handled inside NavigateToSceneAsync which
            // calls the API to progress the session with proper identifiers

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

    public async Task MakeChoiceAsync(
        string gameSessionId,
        string currentSceneId,
        string choiceText,
        string choiceNextSceneId,
        string? playerId = null,
        string? compassAxis = null,
        string? compassDirection = null,
        double? compassDelta = null)
    {
        try
        {
            if (CurrentGameSession == null)
            {
                _logger.LogWarning("Cannot make choice - no active game session");
                return;
            }

            // Resolve the next scene by its Id
            var nextScene = CurrentGameSession.Scenario.Scenes
                .FirstOrDefault(s => string.Equals(s.Id, choiceNextSceneId, StringComparison.OrdinalIgnoreCase));

            if (nextScene == null)
            {
                _logger.LogWarning("Next scene with id '{NextSceneId}' not found in scenario {ScenarioId}",
                    choiceNextSceneId, CurrentGameSession.ScenarioId);
                return;
            }

            _logger.LogInformation("Making choice '{ChoiceText}' to navigate to scene '{NextSceneTitle}' ({NextSceneId})",
                choiceText, nextScene.Title, nextScene.Id);

            var effectivePlayerId = !string.IsNullOrWhiteSpace(playerId)
                ? playerId
                : CurrentGameSession.ProfileId;

            // Notify the server about the choice that was made
            var updatedSession = await _apiClient.MakeChoiceAsync(
                gameSessionId,
                currentSceneId,
                choiceText,
                nextScene.Id,
                effectivePlayerId,
                compassAxis,
                compassDirection,
                compassDelta);

            if (updatedSession == null)
            {
                _logger.LogWarning("API did not return an updated session after making choice. Continuing locally.");
            }
            else
            {
                // Optionally update any server-side fields (keep local scenario and scenes)
                CurrentGameSession.StartedAt = updatedSession.StartedAt;
                CurrentGameSession.IsCompleted = updatedSession.IsCompleted;
            }

            // Then navigate to the resolved next scene (this will also progress the session on the server)
            await NavigateToSceneAsync(nextScene.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error making choice '{ChoiceText}' and navigating to next scene", choiceText);
        }
    }

    public void ClearGameSession()
    {
        _logger.LogInformation("Clearing game session");
        CurrentGameSession = null;
        _characterAssignments.Clear();
        _isPaused = false;
    }

    public void SetCurrentGameSession(GameSession? session)
    {
        _logger.LogInformation("Setting current game session: {SessionId}", session?.Id ?? "null");
        CurrentGameSession = session;
        // Hydrate character assignments cache for text replacement
        _characterAssignments = session?.CharacterAssignments?.Any() == true
            ? new List<CharacterAssignment>(session!.CharacterAssignments)
            : new List<CharacterAssignment>();
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
    /// Replaces character placeholders in text.
    /// Supported forms:
    /// - [c:CharacterName] (case-insensitive): resolved using current character assignments
    /// - [c:*]            : resolved to the active scene character's assigned player name
    /// Falls back to "Player" only if a mapping cannot be resolved.
    /// </summary>
    public string ReplaceCharacterPlaceholders(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        // Helper to compute the display name from a CharacterAssignment using existing rules
        string ResolvePlayerName(CharacterAssignment ca)
        {
            if (ca.PlayerAssignment == null)
                return "Player";

            var characterNameLc = ca.CharacterName?.ToLower() ?? string.Empty;
            var playerProfileName = ca.PlayerAssignment.ProfileName;
            var guestProfileName = ca.PlayerAssignment.GuestName;
            return ca.PlayerAssignment.Type switch
            {
                "Player" => playerProfileName?.ToLower() == characterNameLc
                    ? "Player"
                    : playerProfileName ?? "Player",
                "Profile" => playerProfileName?.ToLower() == characterNameLc
                    ? "Player"
                    : playerProfileName ?? "Player",
                "Guest" => guestProfileName?.ToLower() == characterNameLc
                    ? "Guest"
                    : guestProfileName ?? "Guest",
                _ => "Player"
            };
        }

        // Build quick lookup for assignments by character name (case-insensitive)
        var assignmentsByName = new Dictionary<string, CharacterAssignment>(StringComparer.OrdinalIgnoreCase);
        foreach (var ca in _characterAssignments)
        {
            if (!string.IsNullOrWhiteSpace(ca.CharacterName) && !assignmentsByName.ContainsKey(ca.CharacterName))
            {
                assignmentsByName[ca.CharacterName] = ca;
            }
        }

        // Determine active character assignment for fallback
        CharacterAssignment? activeAssignment = null;
        var activeId = CurrentGameSession?.CurrentScene?.ActiveCharacter;
        if (!string.IsNullOrWhiteSpace(activeId))
        {
            activeAssignment = _characterAssignments.FirstOrDefault(a =>
                string.Equals(a.CharacterId, activeId, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(a.CharacterName, activeId, StringComparison.OrdinalIgnoreCase));
        }

        // Replace all [c:...] tokens in one regex-driven pass, case-insensitive
        text = ParseCharacterRegex().Replace(text, match =>
            {
                var key = match.Groups[1].Value;
                if (string.IsNullOrWhiteSpace(key))
                    return "Player";

                // 1) Try explicit character name match first (existing logic)
                if (assignmentsByName.TryGetValue(key, out var caByName))
                {
                    return ResolvePlayerName(caByName);
                }

                // 2) If [c:*], fall back to active character's assignment
                if (key == "*")
                {
                    if (activeAssignment != null)
                    {
                        return ResolvePlayerName(activeAssignment);
                    }
                    return "Player";
                }

                // 3) Try matching by character id as a convenience (if content uses ids)
                var caById = _characterAssignments.FirstOrDefault(a =>
                    string.Equals(a.CharacterId, key, StringComparison.OrdinalIgnoreCase));
                if (caById != null)
                {
                    return ResolvePlayerName(caById);
                }

                // 4) Finally, if nothing matched, only then fallback to "Player"
                return "Player";
            });

        return text;
    }

    [GeneratedRegex(@"\[c:([^\]]+)\]", RegexOptions.IgnoreCase, "en-GB")]
    private static partial Regex ParseCharacterRegex();
}
