namespace Mystira.DevHub.Services.Infrastructure;

public interface IInfrastructureService
{
    /// <summary>
    /// Triggers infrastructure validation workflow
    /// </summary>
    Task<InfrastructureResult> ValidateAsync(string workflowFile = "infrastructure-deploy-dev.yml", string repository = "phoenixvc/Mystira.App");

    /// <summary>
    /// Triggers infrastructure preview (what-if) workflow
    /// </summary>
    Task<InfrastructureResult> PreviewAsync(string workflowFile = "infrastructure-deploy-dev.yml", string repository = "phoenixvc/Mystira.App");

    /// <summary>
    /// Triggers infrastructure deployment workflow
    /// </summary>
    Task<InfrastructureResult> DeployAsync(string workflowFile = "infrastructure-deploy-dev.yml", string repository = "phoenixvc/Mystira.App");

    /// <summary>
    /// Triggers infrastructure destroy workflow
    /// </summary>
    Task<InfrastructureResult> DestroyAsync(string workflowFile = "infrastructure-deploy-dev.yml", string repository = "phoenixvc/Mystira.App", bool confirm = false);

    /// <summary>
    /// Gets the status of a workflow run
    /// </summary>
    Task<WorkflowStatus> GetWorkflowStatusAsync(string workflowFile, string repository = "phoenixvc/Mystira.App");
}

public class InfrastructureResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? WorkflowRunId { get; set; }
    public string? WorkflowUrl { get; set; }
    public string? Error { get; set; }
}

public class WorkflowStatus
{
    public string Status { get; set; } = string.Empty; // queued, in_progress, completed
    public string Conclusion { get; set; } = string.Empty; // success, failure, cancelled, skipped
    public string WorkflowName { get; set; } = string.Empty;
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string HtmlUrl { get; set; } = string.Empty;
    public List<WorkflowStep> Steps { get; set; } = new();
}

public class WorkflowStep
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // queued, in_progress, completed
    public string Conclusion { get; set; } = string.Empty; // success, failure, cancelled, skipped
    public int Number { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
