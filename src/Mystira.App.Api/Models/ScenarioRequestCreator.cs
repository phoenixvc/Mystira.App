using Mystira.App.Api.Models.Parsers;
using Mystira.App.Contracts.Requests.Scenarios;

namespace Mystira.App.Api.Models;

/// <summary>
/// Facade for creating CreateScenarioRequest from dictionary data
/// Delegates to specialized parsers
/// </summary>
public static class ScenarioRequestCreator
{
    public static CreateScenarioRequest Create(Dictionary<object, object> scenarioData)
    {
        return ScenarioParser.Create(scenarioData);
    }

    public static Scene ParseSceneFromDictionary(IDictionary<object, object> sceneDict)
    {
        return SceneParser.Parse(sceneDict);
    }
}
