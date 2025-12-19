using Mystira.App.Application.Parsers;
using Mystira.App.Contracts.Requests.Scenarios;
using Mystira.App.Domain.Models;

namespace Mystira.App.Api.Models;

/// <summary>
/// Facade for creating CreateScenarioRequest from dictionary data
/// Delegates to specialized parsers in Application layer
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
