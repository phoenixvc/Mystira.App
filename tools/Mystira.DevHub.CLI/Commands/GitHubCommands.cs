using System.Diagnostics;
using System.Text.Json;
using Mystira.DevHub.CLI.Models;

namespace Mystira.DevHub.CLI.Commands;

public class GitHubCommands
{
    public async Task<CommandResponse> ListDeploymentsAsync(JsonElement argsJson)
    {
        try
        {
            var repository = argsJson.GetProperty("repository").GetString();
            var limit = argsJson.TryGetProperty("limit", out var limitProp)
                ? limitProp.GetInt32()
                : 10;

            if (string.IsNullOrEmpty(repository))
            {
                return CommandResponse.Fail("Repository is required");
            }

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "gh",
                    Arguments = $"api repos/{repository}/actions/runs --jq '.workflow_runs[:{ limit}] | map({{id: .id, name: .name, status: .status, conclusion: .conclusion, created_at: .created_at, updated_at: .updated_at, html_url: .html_url, head_branch: .head_branch, event: .event}})'",
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
                return CommandResponse.Fail($"GitHub CLI failed: {error}");
            }

            // Parse the JSON array from gh CLI
            var deployments = JsonSerializer.Deserialize<JsonElement>(output);

            return CommandResponse.Ok(deployments, "Successfully retrieved GitHub deployments");
        }
        catch (Exception ex)
        {
            return CommandResponse.Fail($"Failed to list GitHub deployments: {ex.Message}");
        }
    }
}
