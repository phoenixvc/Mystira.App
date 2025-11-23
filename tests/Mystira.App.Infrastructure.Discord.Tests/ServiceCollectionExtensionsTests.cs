using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mystira.App.Infrastructure.Discord.Configuration;
using Mystira.App.Infrastructure.Discord.HealthChecks;
using Mystira.App.Infrastructure.Discord.Services;
using Xunit;

namespace Mystira.App.Infrastructure.Discord.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddDiscordBot_ShouldRegisterRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Discord:BotToken"] = "test-token",
                ["Discord:EnableMessageContentIntent"] = "true"
            })
            .Build();

        // Add logging (required dependency)
        services.AddLogging();

        // Act
        services.AddDiscordBot(configuration);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        
        // Check that IDiscordBotService is registered
        var discordService = serviceProvider.GetService<IDiscordBotService>();
        discordService.Should().NotBeNull();
        discordService.Should().BeOfType<DiscordBotService>();
    }

    [Fact]
    public void AddDiscordBot_ShouldConfigureOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Discord:BotToken"] = "test-token-123",
                ["Discord:EnableMessageContentIntent"] = "false",
                ["Discord:CommandPrefix"] = "?"
            })
            .Build();

        // Add logging (required dependency)
        services.AddLogging();

        // Act
        services.AddDiscordBot(configuration);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<DiscordOptions>>().Value;
        
        options.BotToken.Should().Be("test-token-123");
        options.EnableMessageContentIntent.Should().BeFalse();
        options.CommandPrefix.Should().Be("?");
    }

    [Fact]
    public void AddDiscordBotHostedService_ShouldRegisterHostedService()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Discord:BotToken"] = "test-token"
            })
            .Build();

        // Add logging (required dependency)
        services.AddLogging();

        // Act
        services.AddDiscordBot(configuration);
        services.AddDiscordBotHostedService();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var hostedServices = serviceProvider.GetServices<Microsoft.Extensions.Hosting.IHostedService>();
        
        hostedServices.Should().Contain(s => s.GetType() == typeof(DiscordBotHostedService));
    }

    [Fact]
    public void AddDiscordBotHealthCheck_ShouldRegisterHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Discord:BotToken"] = "test-token"
            })
            .Build();

        // Add logging (required dependency)
        services.AddLogging();
        services.AddDiscordBot(configuration);

        // Act
        services.AddHealthChecks()
            .AddDiscordBotHealthCheck();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var healthCheckService = serviceProvider.GetService<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckService>();
        
        healthCheckService.Should().NotBeNull();
    }
}
