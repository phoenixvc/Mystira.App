namespace Mystira.Contracts.App.Responses.Scenarios;

public class ScenarioListResponse
{
    public List<ScenarioSummary> Scenarios { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public bool HasNextPage { get; set; }
}

