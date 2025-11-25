using Mystira.App.Contracts.Requests.Scenarios;
using Mystira.App.Contracts.Responses.Scenarios;
using Mystira.App.Domain.Models;

namespace Mystira.App.Api.Services;

/// <summary>
/// DEPRECATED: This service violates hexagonal architecture.
/// Controllers should use IMediator (CQRS pattern) instead.
/// </summary>
/// <remarks>
/// Migration guide:
/// - GetScenariosAsync → GetPaginatedScenariosQuery
/// - GetScenarioByIdAsync → GetScenarioQuery
/// - GetScenariosByAgeGroupAsync → GetScenariosByAgeGroupQuery
/// - GetFeaturedScenariosAsync → GetFeaturedScenariosQuery
/// - GetScenariosWithGameStateAsync → GetScenariosWithGameStateQuery
/// - CreateScenarioAsync → CreateScenarioCommand
/// - DeleteScenarioAsync → DeleteScenarioCommand
/// See ARCHITECTURAL_REFACTORING_PLAN.md for details.
/// </remarks>
[Obsolete("Use IMediator with CQRS queries/commands instead. See ARCHITECTURAL_REFACTORING_PLAN.md")]
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
    Task<ScenarioGameStateResponse> GetScenariosWithGameStateAsync(string accountId);
}
