using Mystira.App.PWA.Models;

namespace Mystira.App.PWA.Services;

public interface IApiClient
{
    Task<List<Scenario>> GetScenariosAsync();
    Task<Scenario?> GetScenarioAsync(string id);
    Task<Scene?> GetSceneAsync(string scenarioId, string sceneId);
    Task<string?> GetMediaUrlFromId(string mediaAudio);
}
