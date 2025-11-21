using Mystira.App.Api.Models;
using Mystira.App.Domain.Models;

namespace Mystira.App.Api.Services;

public interface IScenarioApiService
{
    Task<ScenarioListResponse> GetScenariosAsync(ScenarioQueryRequest request);
    Task<Scenario?> GetScenarioByIdAsync(string id);
    Task<Scenario> CreateScenarioAsync(CreateScenarioRequest request);
    Task<Scenario?> UpdateScenarioAsync(string id, CreateScenarioRequest request);
    Task<bool> DeleteScenarioAsync(string id);
    Task<List<Scenario>> GetScenariosByAgeGroupAsync(string ageGroup);
    Task<List<Scenario>> GetFeaturedScenariosAsync();
    Task ValidateScenarioAsync(Scenario scenario);
    Task<ScenarioReferenceValidation> ValidateScenarioReferencesAsync(string scenarioId, bool includeMetadataValidation = true);
    Task<List<ScenarioReferenceValidation>> ValidateAllScenarioReferencesAsync(bool includeMetadataValidation = true);
}
