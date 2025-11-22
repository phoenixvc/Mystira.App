using Mystira.App.PWA.Models;

namespace Mystira.App.PWA.Services;

public interface IScenarioApiClient
{
    Task<List<Scenario>> GetScenariosAsync();
    Task<Scenario?> GetScenarioAsync(string id);
    Task<Scene?> GetSceneAsync(string scenarioId, string sceneId);
    Task<ScenarioGameStateResponse?> GetScenariosWithGameStateAsync(string accountId);
    Task<bool> CompleteScenarioForAccountAsync(string accountId, string scenarioId);
}

