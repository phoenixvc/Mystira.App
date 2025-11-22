using Mystira.App.Admin.Api.Models;
using Mystira.App.Contracts.Requests.Scenarios;
using Mystira.App.Contracts.Responses.Scenarios;
using Mystira.App.Domain.Models;
using ScenarioQueryRequest = Mystira.App.Contracts.Requests.Scenarios.ScenarioQueryRequest;
using ScenarioListResponse = Mystira.App.Contracts.Responses.Scenarios.ScenarioListResponse;
using CreateScenarioRequest = Mystira.App.Contracts.Requests.Scenarios.CreateScenarioRequest;
using ScenarioReferenceValidation = Mystira.App.Contracts.Responses.Scenarios.ScenarioReferenceValidation;

namespace Mystira.App.Admin.Api.Services;

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
