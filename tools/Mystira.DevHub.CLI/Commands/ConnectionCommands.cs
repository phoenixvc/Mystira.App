using System.Diagnostics;
using System.Text.Json;
using Microsoft.Azure.Cosmos;
using Azure.Storage.Blobs;
using Mystira.DevHub.CLI.Models;

namespace Mystira.DevHub.CLI.Commands;

public class ConnectionCommands
{
    public async Task<CommandResponse> TestAsync(JsonElement argsJson)
    {
        try
        {
            var type = argsJson.GetProperty("type").GetString()?.ToLower();
            var connectionString = argsJson.TryGetProperty("connectionString", out var connProp)
                ? connProp.GetString()
                : null;

            return type switch
            {
                "cosmos" => await TestCosmosConnectionAsync(connectionString),
                "storage" => await TestStorageConnectionAsync(connectionString),
                "azurecli" => await TestAzureCliAsync(),
                "githubcli" => await TestGitHubCliAsync(),
                _ => CommandResponse.Fail($"Unknown connection type: {type}")
            };
        }
        catch (Exception ex)
        {
            return CommandResponse.Fail($"Connection test failed: {ex.Message}");
        }
    }

    private async Task<CommandResponse> TestCosmosConnectionAsync(string? connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            return CommandResponse.Fail("Connection string is required for Cosmos DB test");
        }

        try
        {
            var client = new CosmosClient(connectionString);
            var response = await client.ReadAccountAsync();

            return CommandResponse.Ok(new
            {
                connected = true,
                accountName = response.Resource.Id,
                consistencyLevel = response.Resource.Consistency.DefaultConsistencyLevel.ToString(),
                regions = response.Resource.ReadableRegions.Select(r => r.Name).ToList()
            }, "Cosmos DB connection successful");
        }
        catch (Exception ex)
        {
            return CommandResponse.Fail($"Cosmos DB connection failed: {ex.Message}");
        }
    }

    private async Task<CommandResponse> TestStorageConnectionAsync(string? connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            return CommandResponse.Fail("Connection string is required for Storage test");
        }

        try
        {
            var serviceClient = new BlobServiceClient(connectionString);
            var properties = await serviceClient.GetPropertiesAsync();

            return CommandResponse.Ok(new
            {
                connected = true,
                accountName = serviceClient.AccountName,
                sku = properties.Value.SkuName
            }, "Storage connection successful");
        }
        catch (Exception ex)
        {
            return CommandResponse.Fail($"Storage connection failed: {ex.Message}");
        }
    }

    private async Task<CommandResponse> TestAzureCliAsync()
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "az",
                    Arguments = "account show --output json",
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
                return CommandResponse.Fail("Azure CLI not authenticated. Run: az login");
            }

            var account = JsonSerializer.Deserialize<JsonElement>(output);
            var accountName = account.GetProperty("user").GetProperty("name").GetString();
            var subscriptionName = account.GetProperty("name").GetString();

            return CommandResponse.Ok(new
            {
                connected = true,
                user = accountName,
                subscription = subscriptionName
            }, "Azure CLI authenticated");
        }
        catch (Exception ex)
        {
            return CommandResponse.Fail($"Azure CLI test failed: {ex.Message}");
        }
    }

    private async Task<CommandResponse> TestGitHubCliAsync()
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "gh",
                    Arguments = "auth status",
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
                return CommandResponse.Fail("GitHub CLI not authenticated. Run: gh auth login");
            }

            // gh auth status writes to stderr, not stdout
            var statusText = error + output;

            return CommandResponse.Ok(new
            {
                connected = true,
                status = statusText.Contains("Logged in") ? "authenticated" : "unknown"
            }, "GitHub CLI authenticated");
        }
        catch (Exception ex)
        {
            return CommandResponse.Fail($"GitHub CLI test failed: {ex.Message}");
        }
    }
}
