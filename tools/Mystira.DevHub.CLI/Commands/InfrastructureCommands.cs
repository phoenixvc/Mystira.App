using System.Text.Json;
using Mystira.DevHub.CLI.Models;
using Mystira.DevHub.Services.Infrastructure;

namespace Mystira.DevHub.CLI.Commands;

public class InfrastructureCommands
{
    private readonly IInfrastructureService _infrastructureService;

    public InfrastructureCommands(IInfrastructureService infrastructureService)
    {
        _infrastructureService = infrastructureService;
    }

    public async Task<CommandResponse> ValidateAsync(JsonElement argsJson)
    {
        try
        {
            var args = JsonSerializer.Deserialize<InfrastructureArgs>(argsJson.GetRawText());
            if (args == null)
            {
                return CommandResponse.Fail("Invalid arguments");
            }

            var result = await _infrastructureService.ValidateAsync(args.WorkflowFile, args.Repository);

            if (result.Success)
            {
                return CommandResponse.Ok(result, result.Message);
            }
            else
            {
                return CommandResponse.Fail(result.Error ?? "Validation failed", result.Message);
            }
        }
        catch (Exception ex)
        {
            return CommandResponse.Fail(ex.Message);
        }
    }

    public async Task<CommandResponse> PreviewAsync(JsonElement argsJson)
    {
        try
        {
            var args = JsonSerializer.Deserialize<InfrastructureArgs>(argsJson.GetRawText());
            if (args == null)
            {
                return CommandResponse.Fail("Invalid arguments");
            }

            var result = await _infrastructureService.PreviewAsync(args.WorkflowFile, args.Repository);

            if (result.Success)
            {
                return CommandResponse.Ok(result, result.Message);
            }
            else
            {
                return CommandResponse.Fail(result.Error ?? "Preview failed", result.Message);
            }
        }
        catch (Exception ex)
        {
            return CommandResponse.Fail(ex.Message);
        }
    }

    public async Task<CommandResponse> DeployAsync(JsonElement argsJson)
    {
        try
        {
            var args = JsonSerializer.Deserialize<InfrastructureArgs>(argsJson.GetRawText());
            if (args == null)
            {
                return CommandResponse.Fail("Invalid arguments");
            }

            var result = await _infrastructureService.DeployAsync(args.WorkflowFile, args.Repository);

            if (result.Success)
            {
                return CommandResponse.Ok(result, result.Message);
            }
            else
            {
                return CommandResponse.Fail(result.Error ?? "Deployment failed", result.Message);
            }
        }
        catch (Exception ex)
        {
            return CommandResponse.Fail(ex.Message);
        }
    }

    public async Task<CommandResponse> DestroyAsync(JsonElement argsJson)
    {
        try
        {
            var args = JsonSerializer.Deserialize<InfrastructureArgs>(argsJson.GetRawText());
            if (args == null)
            {
                return CommandResponse.Fail("Invalid arguments");
            }

            if (!args.Confirm)
            {
                return CommandResponse.Fail("Destroy operation requires explicit confirmation (set confirm: true)");
            }

            var result = await _infrastructureService.DestroyAsync(args.WorkflowFile, args.Repository, args.Confirm);

            if (result.Success)
            {
                return CommandResponse.Ok(result, result.Message);
            }
            else
            {
                return CommandResponse.Fail(result.Error ?? "Destroy failed", result.Message);
            }
        }
        catch (Exception ex)
        {
            return CommandResponse.Fail(ex.Message);
        }
    }

    public async Task<CommandResponse> StatusAsync(JsonElement argsJson)
    {
        try
        {
            var args = JsonSerializer.Deserialize<InfrastructureArgs>(argsJson.GetRawText());
            if (args == null)
            {
                return CommandResponse.Fail("Invalid arguments");
            }

            var status = await _infrastructureService.GetWorkflowStatusAsync(args.WorkflowFile, args.Repository);

            return CommandResponse.Ok(status, $"Workflow status: {status.Status}");
        }
        catch (Exception ex)
        {
            return CommandResponse.Fail(ex.Message);
        }
    }
}
