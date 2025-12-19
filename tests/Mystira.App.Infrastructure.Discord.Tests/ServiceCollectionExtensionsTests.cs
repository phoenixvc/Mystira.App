using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Mystira.App.Application.Ports.Messaging;
using Mystira.App.Infrastructure.Discord.Configuration;
using Mystira.App.Infrastructure.Discord.Services;

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

        // Check that IChatBotService is registered
        var chatBotService = serviceProvider.GetService<IChatBotService>();
        chatBotService.Should().NotBeNull();
        chatBotService.Should().BeOfType<DiscordBotService>();
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
        var hostedServices = serviceProvider.GetServices<IHostedService>();

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
        var healthCheckService = serviceProvider.GetService<HealthCheckService>();

        healthCheckService.Should().NotBeNull();
    }

    [Fact]
    public void AddDiscordBot_ShouldRegisterIMessagingService()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Discord:BotToken"] = "test-token"
            })
            .Build();

        services.AddLogging();

        // Act
        services.AddDiscordBot(configuration);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var messagingService = serviceProvider.GetService<IMessagingService>();

        messagingService.Should().NotBeNull();
        messagingService.Should().BeOfType<DiscordBotService>();
    }

    [Fact]
    public void AddDiscordBotKeyed_ShouldRegisterKeyedServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Discord:BotToken"] = "test-token"
            })
            .Build();

        services.AddLogging();

        // Act
        services.AddDiscordBotKeyed(configuration);

        // Assert
        var serviceProvider = services.BuildServiceProvider();

        // Verify keyed services are registered
        var keyedMessaging = serviceProvider.GetKeyedService<IMessagingService>("discord");
        var keyedChatBot = serviceProvider.GetKeyedService<IChatBotService>("discord");
        var keyedBotCommand = serviceProvider.GetKeyedService<IBotCommandService>("discord");

        keyedMessaging.Should().NotBeNull();
        keyedChatBot.Should().NotBeNull();
        keyedBotCommand.Should().NotBeNull();

        // All should be the same singleton instance
        keyedMessaging.Should().BeSameAs(keyedChatBot);
        keyedChatBot.Should().BeSameAs(keyedBotCommand);
    }

    [Fact]
    public void AddDiscordBotKeyed_WithCustomKey_ShouldUseCustomKey()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Discord:BotToken"] = "test-token"
            })
            .Build();

        services.AddLogging();

        // Act
        services.AddDiscordBotKeyed(configuration, "my-discord-bot");

        // Assert
        var serviceProvider = services.BuildServiceProvider();

        var keyedService = serviceProvider.GetKeyedService<IChatBotService>("my-discord-bot");
        keyedService.Should().NotBeNull();

        // Default key should not be registered
        var defaultKeyService = serviceProvider.GetKeyedService<IChatBotService>("discord");
        defaultKeyService.Should().BeNull();
    }
}
