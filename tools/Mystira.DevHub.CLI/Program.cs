using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mystira.DevHub.CLI.Commands;
using Mystira.DevHub.CLI.Models;
using Mystira.DevHub.Services.Cosmos;
using Mystira.DevHub.Services.Data;
using Mystira.DevHub.Services.Infrastructure;
using Mystira.DevHub.Services.Migration;

namespace Mystira.DevHub.CLI;

internal class Program
{
    private static async Task<int> Main(string[] args)
    {
        try
        {
            // Build configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            // Setup dependency injection
            var services = new ServiceCollection();

            // Add logging (only errors to console, rest to file/debug)
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Error); // Only show errors in console
            });

            // Add configuration
            services.AddSingleton<IConfiguration>(configuration);

            // Add DbContext (connection string can be overridden by environment)
            var connectionString = Environment.GetEnvironmentVariable("COSMOS_CONNECTION_STRING")
                ?? configuration.GetConnectionString("CosmosDb")
                ?? "";
            var databaseName = Environment.GetEnvironmentVariable("COSMOS_DATABASE_NAME")
                ?? configuration["Database:Name"]
                ?? "MystiraAppDb";

            if (!string.IsNullOrEmpty(connectionString))
            {
                services.AddDbContext<DevHubDbContext>(options =>
                    options.UseCosmos(connectionString, databaseName));
            }

            // Add services
            services.AddScoped<ICosmosReportingService, CosmosReportingService>();
            services.AddScoped<IMigrationService, MigrationService>();
            services.AddScoped<IInfrastructureService, InfrastructureService>();

            // Add command handlers
            services.AddScoped<CosmosCommands>();
            services.AddScoped<MigrationCommands>();
            services.AddScoped<InfrastructureCommands>();

            var serviceProvider = services.BuildServiceProvider();

            // Read JSON command from stdin
            string? jsonInput = null;
            if (args.Length > 0)
            {
                // Command provided as argument (for testing)
                jsonInput = string.Join(" ", args);
            }
            else
            {
                // Read from stdin (normal mode for Tauri)
                jsonInput = await Console.In.ReadToEndAsync();
            }

            if (string.IsNullOrWhiteSpace(jsonInput))
            {
                var errorResponse = CommandResponse.Fail("No input provided");
                Console.WriteLine(JsonSerializer.Serialize(errorResponse));
                return 1;
            }

            // Parse the command
            CommandRequest? request;
            try
            {
                request = JsonSerializer.Deserialize<CommandRequest>(jsonInput);
                if (request == null)
                {
                    var errorResponse = CommandResponse.Fail("Invalid JSON input");
                    Console.WriteLine(JsonSerializer.Serialize(errorResponse));
                    return 1;
                }
            }
            catch (JsonException ex)
            {
                var errorResponse = CommandResponse.Fail($"JSON parse error: {ex.Message}");
                Console.WriteLine(JsonSerializer.Serialize(errorResponse));
                return 1;
            }

            // Route the command to the appropriate handler
            CommandResponse response;
            try
            {
                response = request.Command.ToLower() switch
                {
                    "cosmos.export" => await serviceProvider.GetRequiredService<CosmosCommands>().ExportAsync(request.Args),
                    "cosmos.stats" => await serviceProvider.GetRequiredService<CosmosCommands>().StatsAsync(request.Args),
                    "migration.run" => await serviceProvider.GetRequiredService<MigrationCommands>().RunAsync(request.Args),
                    "infrastructure.validate" => await serviceProvider.GetRequiredService<InfrastructureCommands>().ValidateAsync(request.Args),
                    "infrastructure.preview" => await serviceProvider.GetRequiredService<InfrastructureCommands>().PreviewAsync(request.Args),
                    "infrastructure.deploy" => await serviceProvider.GetRequiredService<InfrastructureCommands>().DeployAsync(request.Args),
                    "infrastructure.destroy" => await serviceProvider.GetRequiredService<InfrastructureCommands>().DestroyAsync(request.Args),
                    "infrastructure.status" => await serviceProvider.GetRequiredService<InfrastructureCommands>().StatusAsync(request.Args),
                    _ => CommandResponse.Fail($"Unknown command: {request.Command}")
                };
            }
            catch (Exception ex)
            {
                response = CommandResponse.Fail(ex.Message, ex.StackTrace);
            }

            // Write response as JSON to stdout
            var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });
            Console.WriteLine(jsonResponse);

            return response.Success ? 0 : 1;
        }
        catch (Exception ex)
        {
            // Last-resort error handler
            var errorResponse = CommandResponse.Fail($"Fatal error: {ex.Message}", ex.StackTrace);
            Console.WriteLine(JsonSerializer.Serialize(errorResponse));
            return 1;
        }
    }
}
