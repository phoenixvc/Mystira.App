using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;

namespace Mystira.DevHub.Services.Infrastructure;

public class InfrastructureService : IInfrastructureService
{
    private readonly ILogger<InfrastructureService> _logger;

    public InfrastructureService(ILogger<InfrastructureService> logger)
    {
        _logger = logger;
    }

    public async Task<InfrastructureResult> ValidateAsync(string workflowFile = "infrastructure-deploy-dev.yml", string repository = "phoenixvc/Mystira.App")
    {
        return await TriggerWorkflowAsync("validate", workflowFile, repository);
    }

    public async Task<InfrastructureResult> PreviewAsync(string workflowFile = "infrastructure-deploy-dev.yml", string repository = "phoenixvc/Mystira.App")
    {
        return await TriggerWorkflowAsync("preview", workflowFile, repository);
    }

    public async Task<InfrastructureResult> DeployAsync(string workflowFile = "infrastructure-deploy-dev.yml", string repository = "phoenixvc/Mystira.App")
    {
        return await TriggerWorkflowAsync("deploy", workflowFile, repository);
    }

    public async Task<InfrastructureResult> DestroyAsync(string workflowFile = "infrastructure-deploy-dev.yml", string repository = "phoenixvc/Mystira.App", bool confirm = false)
    {
        if (!confirm)
        {
            return new InfrastructureResult
            {
                Success = false,
                Error = "Destroy operation requires explicit confirmation"
            };
        }

        return await TriggerWorkflowAsync("destroy", workflowFile, repository, new Dictionary<string, string>
        {
            { "destroyInfrastructure", "true" }
        });
    }

    public async Task<WorkflowStatus> GetWorkflowStatusAsync(string workflowFile, string repository = "phoenixvc/Mystira.App")
    {
        try
        {
            _logger.LogInformation("Getting workflow status for {WorkflowFile}", workflowFile);

            // Get the most recent workflow run
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "gh",
                    Arguments = $"run list --workflow={workflowFile} --repo {repository} --limit 1 --json status,conclusion,name,createdAt,updatedAt,url",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                _logger.LogError("Failed to get workflow status: {Error}", error);
                return new WorkflowStatus { Status = "error" };
            }

            var runs = JsonSerializer.Deserialize<List<JsonElement>>(output);
            if (runs == null || runs.Count == 0)
            {
                return new WorkflowStatus { Status = "not_found" };
            }

            var run = runs[0];
            return new WorkflowStatus
            {
                Status = run.GetProperty("status").GetString() ?? "",
                Conclusion = run.TryGetProperty("conclusion", out var conclusion) ? conclusion.GetString() ?? "" : "",
                WorkflowName = run.GetProperty("name").GetString() ?? "",
                CreatedAt = run.TryGetProperty("createdAt", out var createdAt) ? DateTime.Parse(createdAt.GetString()!) : null,
                UpdatedAt = run.TryGetProperty("updatedAt", out var updatedAt) ? DateTime.Parse(updatedAt.GetString()!) : null,
                HtmlUrl = run.GetProperty("url").GetString() ?? ""
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting workflow status");
            return new WorkflowStatus { Status = "error" };
        }
    }

    private async Task<InfrastructureResult> TriggerWorkflowAsync(string action, string workflowFile, string repository, Dictionary<string, string>? additionalInputs = null)
    {
        try
        {
            _logger.LogInformation("Triggering infrastructure workflow: {Action}", action);

            // Check if gh CLI is installed
            if (!await IsGitHubCliInstalledAsync())
            {
                return new InfrastructureResult
                {
                    Success = false,
                    Error = "GitHub CLI (gh) is not installed. Install it from: https://cli.github.com/"
                };
            }

            // Build the gh workflow run command
            var arguments = $"workflow run \"{workflowFile}\" --field action={action} --repo {repository}";

            // Add additional inputs if provided
            if (additionalInputs != null)
            {
                foreach (var input in additionalInputs)
                {
                    arguments += $" --field {input.Key}={input.Value}";
                }
            }

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "gh",
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                _logger.LogError("Failed to trigger workflow: {Error}", error);
                return new InfrastructureResult
                {
                    Success = false,
                    Error = error,
                    Message = "Failed to trigger workflow"
                };
            }

            _logger.LogInformation("Infrastructure workflow triggered successfully");

            return new InfrastructureResult
            {
                Success = true,
                Message = $"Workflow triggered successfully with action: {action}",
                WorkflowUrl = $"https://github.com/{repository}/actions/workflows/{workflowFile}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering infrastructure workflow");
            return new InfrastructureResult
            {
                Success = false,
                Error = ex.Message,
                Message = "An error occurred while triggering the workflow"
            };
        }
    }

    private async Task<bool> IsGitHubCliInstalledAsync()
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "gh",
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            await process.WaitForExitAsync();

            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
