namespace Mystira.App.Contracts.Requests.Scenarios;

/// <summary>
/// Request model for validating scenario references
/// </summary>
public class ValidateScenarioReferencesRequest
{
    public string ScenarioId { get; set; } = string.Empty;
    public bool IncludeMetadataValidation { get; set; } = true;
}

