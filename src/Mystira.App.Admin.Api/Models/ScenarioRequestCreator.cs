using Mystira.App.Application.Parsers;
using Mystira.App.Contracts.Requests.Scenarios;
using Mystira.App.Domain.Models;
using ContractsCreateScenarioRequest = Mystira.App.Contracts.Requests.Scenarios.CreateScenarioRequest;

namespace Mystira.App.Admin.Api.Models;

/// <summary>
/// Facade for creating CreateScenarioRequest from dictionary data
/// Delegates to specialized parsers in Application layer
/// </summary>
public static class ScenarioRequestCreator
{
    public static ContractsCreateScenarioRequest Create(Dictionary<object, object> scenarioData)
    {
        return ScenarioParser.Create(scenarioData);
    }

    public static Domain.Models.Scene ParseSceneFromDictionary(IDictionary<object, object> sceneDict)
    {
        return SceneParser.Parse(sceneDict);
    }
}
