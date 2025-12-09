using System.Diagnostics;
using System.Text.Json;
using Mystira.DevHub.CLI.Models;

namespace Mystira.DevHub.CLI.Commands;

public class AzureCommands
{
    public async Task<CommandResponse> ListResourcesAsync(JsonElement argsJson)
    {
        try
        {
            var subscriptionId = argsJson.TryGetProperty("subscriptionId", out var subIdProp)
                ? subIdProp.GetString()
                : null;

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "az",
                    Arguments = "resource list --output json",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            // Add subscription filter if provided
            if (!string.IsNullOrEmpty(subscriptionId))
            {
                process.StartInfo.Arguments += $" --subscription {subscriptionId}";
            }

            process.Start();

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                return CommandResponse.Fail($"Azure CLI failed: {error}");
            }

            // Parse the JSON array from az CLI
            var resources = JsonSerializer.Deserialize<JsonElement>(output);

            return CommandResponse.Ok(resources, "Successfully retrieved Azure resources");
        }
        catch (Exception ex)
        {
            return CommandResponse.Fail($"Failed to list Azure resources: {ex.Message}");
        }
    }
}
