using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Mystira.App.Infrastructure.Discord;

Console.WriteLine("=== Discord Bot Example ===");
Console.WriteLine("Starting Discord bot...");
Console.WriteLine();

var builder = Host.CreateApplicationBuilder(args);

// Add Discord bot services
builder.Services.AddDiscordBot(builder.Configuration);
builder.Services.AddDiscordBotHostedService();

// Add health checks (optional)
builder.Services.AddHealthChecks()
    .AddDiscordBotHealthCheck();

var host = builder.Build();

Console.WriteLine("Discord bot initialized. Press Ctrl+C to stop.");
Console.WriteLine("Configure your bot token in appsettings.json or user secrets:");
Console.WriteLine("  dotnet user-secrets set \"Discord:BotToken\" \"YOUR_BOT_TOKEN_HERE\"");
Console.WriteLine();

try
{
    await host.RunAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    Console.WriteLine();
    Console.WriteLine("Make sure you have configured the Discord bot token:");
    Console.WriteLine("  1. Create a bot at https://discord.com/developers/applications");
    Console.WriteLine("  2. Copy the bot token");
    Console.WriteLine("  3. Set it using: dotnet user-secrets set \"Discord:BotToken\" \"YOUR_TOKEN\"");
    return 1;
}

return 0;
