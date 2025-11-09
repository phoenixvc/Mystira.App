using Mystira.PWA.Models;

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
}
