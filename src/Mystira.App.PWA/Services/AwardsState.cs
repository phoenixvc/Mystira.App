using Mystira.App.PWA.Models;

namespace Mystira.App.PWA.Services;

public interface IAwardsState
{
    FinalizeSessionResponse? Result { get; }
    string? ScenarioId { get; }
    string? ScenarioAgeGroup { get; }
    void Set(FinalizeSessionResponse result, string? scenarioId = null, string? scenarioAgeGroup = null);
    void Clear();
}

public class AwardsState : IAwardsState
{
    public FinalizeSessionResponse? Result { get; private set; }
    public string? ScenarioId { get; private set; }
    public string? ScenarioAgeGroup { get; private set; }

    public void Set(FinalizeSessionResponse result, string? scenarioId = null, string? scenarioAgeGroup = null)
    {
        Result = result;
        ScenarioId = scenarioId;
        ScenarioAgeGroup = scenarioAgeGroup;
    }

    public void Clear()
    {
        Result = null;
        ScenarioId = null;
        ScenarioAgeGroup = null;
    }
}
