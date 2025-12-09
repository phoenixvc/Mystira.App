using System.Reflection;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Discord.ExampleBot;

public class Program
{
    private DiscordSocketClient _client = null!;
    private InteractionService _interactions = null!;
    private IServiceProvider _services = null!;

    public static Task Main(string[] args) => new Program().RunAsync();

    public async Task RunAsync()
    {
        // Build configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
            .Build();

        var discordSettings = configuration.GetSection("Discord").Get<DiscordSettings>()
                              ?? throw new InvalidOperationException("Discord settings missing.");

        if (string.IsNullOrWhiteSpace(discordSettings.Token))
            throw new InvalidOperationException("Discord:Token not set.");

        var config = new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.Guilds
        };

        _client = new DiscordSocketClient(config);
        _interactions = new InteractionService(_client.Rest);

        _services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddSingleton(discordSettings)
            .AddSingleton(_client)
            .AddSingleton(_interactions)
            // Logging for your services
            .AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            })
            // Register your startup poster service
            .AddSingleton<SampleTicketStartupService>()
            .BuildServiceProvider();

        _client.Log += msg => { Console.WriteLine(msg); return Task.CompletedTask; };
        _interactions.Log += msg => { Console.WriteLine(msg); return Task.CompletedTask; };

        _client.Ready += () => OnReadyAsync(discordSettings);

        _client.InteractionCreated += async interaction =>
        {
            var ctx = new SocketInteractionContext(_client, interaction);
            await _interactions.ExecuteCommandAsync(ctx, _services);
        };

        await _client.LoginAsync(TokenType.Bot, discordSettings.Token);
        await _client.StartAsync();

        await Task.Delay(-1);
    }

    private async Task OnReadyAsync(DiscordSettings settings)
    {
        // 1) Load slash command modules
        await _interactions.AddModulesAsync(Assembly.GetExecutingAssembly(), _services);

        // 2) Register commands
        if (settings.GuildId != 0)
        {
            await _interactions.RegisterCommandsToGuildAsync(settings.GuildId);
            Console.WriteLine($"Slash commands registered to guild {settings.GuildId}");
        }
        else if (settings.RegisterGloballyIfNoGuildId)
        {
            await _interactions.RegisterCommandsGloballyAsync();
            Console.WriteLine("Slash commands registered globally");
        }
        else
        {
            Console.WriteLine("No GuildId provided. Commands not registered.");
        }

        // 3) Post sample ticket (if enabled)
        var startup = _services.GetRequiredService<SampleTicketStartupService>();
        await startup.PostSampleTicketIfEnabledAsync();
    }
}
