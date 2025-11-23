using System.Text.Json;
using Mystira.DevHub.CLI.Models;
using Mystira.DevHub.Services.Cosmos;
using Mystira.DevHub.Services.Extensions;

namespace Mystira.DevHub.CLI.Commands;

public class CosmosCommands
{
    private readonly ICosmosReportingService _reportingService;

    public CosmosCommands(ICosmosReportingService reportingService)
    {
        _reportingService = reportingService;
    }

    public async Task<CommandResponse> ExportAsync(JsonElement argsJson)
    {
        try
        {
            var args = JsonSerializer.Deserialize<CosmosExportArgs>(argsJson.GetRawText());
            if (args == null || string.IsNullOrEmpty(args.OutputPath))
            {
                return CommandResponse.Fail("OutputPath is required");
            }

            var dataTable = await _reportingService.GetGameSessionReportingTable();
            var csv = dataTable.ToCsv();

            // Write CSV to file
            await File.WriteAllTextAsync(args.OutputPath, csv);

            return CommandResponse.Ok(new
            {
                rowCount = dataTable.Rows.Count,
                outputPath = args.OutputPath
            }, $"Exported {dataTable.Rows.Count} sessions to {args.OutputPath}");
        }
        catch (Exception ex)
        {
            return CommandResponse.Fail(ex.Message);
        }
    }

    public async Task<CommandResponse> StatsAsync(JsonElement argsJson)
    {
        try
        {
            var statistics = await _reportingService.GetScenarioStatisticsAsync();

            return CommandResponse.Ok(statistics, $"Retrieved statistics for {statistics.Count} scenarios");
        }
        catch (Exception ex)
        {
            return CommandResponse.Fail(ex.Message);
        }
    }
}
