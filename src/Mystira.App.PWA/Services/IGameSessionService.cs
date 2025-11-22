using Mystira.App.PWA.Models;

namespace Mystira.App.PWA.Services;

public interface IGameSessionService
{
    event EventHandler<GameSession?>? GameSessionChanged;
    GameSession? CurrentGameSession { get; }

    Task<bool> StartGameSessionAsync(Scenario scenario);
    Task<bool> NavigateToSceneAsync(string sceneId);
    Task<bool> NavigateFromRollAsync(bool isSuccess);
    Task<bool> GoToNextSceneAsync();
    Task<bool> CompleteGameSessionAsync();
    void ClearGameSession();
    void SetCurrentGameSession(GameSession? session);

    /// <summary>
    /// Sets character assignments for the current session (for text replacement)
    /// </summary>
    void SetCharacterAssignments(List<CharacterAssignment> assignments);

    /// <summary>
    /// Replaces character placeholders [c:CharacterName] with player names in text
    /// </summary>
    string ReplaceCharacterPlaceholders(string text);
}
