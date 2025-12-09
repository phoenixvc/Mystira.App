using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Mystira.App.Application.Ports.Messaging;
using Mystira.App.Infrastructure.Discord.Configuration;
using Mystira.App.Infrastructure.Discord.Services;

namespace Mystira.App.Infrastructure.Discord.Tests;

/// <summary>
/// Unit tests for DiscordBotService.
/// Note: Some functionality requires integration tests as Discord.NET client is hard to mock.
/// These tests focus on configuration, state management, and interface compliance.
/// </summary>
public class DiscordBotServiceTests : IDisposable
{
    private readonly Mock<ILogger<DiscordBotService>> _mockLogger;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private DiscordBotService? _service;

    public DiscordBotServiceTests()
    {
        _mockLogger = new Mock<ILogger<DiscordBotService>>();
        _mockServiceProvider = new Mock<IServiceProvider>();
    }

    private DiscordBotService CreateService(DiscordOptions? options = null)
    {
        options ??= new DiscordOptions
        {
            BotToken = "test-token",
            EnableSlashCommands = true,
            MaxRetryAttempts = 3,
            DefaultTimeoutSeconds = 30
        };

        var optionsWrapper = Options.Create(options);
        _service = new DiscordBotService(optionsWrapper, _mockLogger.Object, _mockServiceProvider.Object);
        return _service;
    }

    [Fact]
    public void Constructor_InitializesService()
    {
        // Act
        var service = CreateService();

        // Assert
        service.Should().NotBeNull();
        service.Platform.Should().Be(ChatPlatform.Discord);
    }

    [Fact]
    public void Platform_ReturnsDiscord()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        service.Platform.Should().Be(ChatPlatform.Discord);
    }

    [Fact]
    public void IsConnected_WhenNotStarted_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        service.IsConnected.Should().BeFalse();
    }

    [Fact]
    public async Task StartAsync_WhenBotTokenIsEmpty_ThrowsInvalidOperationException()
    {
        // Arrange
        var options = new DiscordOptions { BotToken = "" };
        var service = CreateService(options);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.StartAsync(CancellationToken.None));
    }

    [Fact]
    public async Task StartAsync_WhenBotTokenIsNull_ThrowsInvalidOperationException()
    {
        // Arrange
        var options = new DiscordOptions { BotToken = null! };
        var service = CreateService(options);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.StartAsync(CancellationToken.None));
    }

    [Fact]
    public async Task StartAsync_WhenBotTokenIsWhitespace_ThrowsInvalidOperationException()
    {
        // Arrange
        var options = new DiscordOptions { BotToken = "   " };
        var service = CreateService(options);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.StartAsync(CancellationToken.None));
    }

    [Fact]
    public void GetStatus_WhenNotConnected_ReturnsCorrectStatus()
    {
        // Arrange
        var options = new DiscordOptions { BotToken = "test-token" };
        var service = CreateService(options);

        // Act
        var status = service.GetStatus();

        // Assert
        status.IsEnabled.Should().BeTrue();
        status.IsConnected.Should().BeFalse();
        status.BotName.Should().BeNull();
        status.BotId.Should().BeNull();
        status.ServerCount.Should().Be(0);
    }

    [Fact]
    public void GetStatus_WhenTokenNotConfigured_ReturnsDisabled()
    {
        // Arrange
        var options = new DiscordOptions { BotToken = "" };
        var service = CreateService(options);

        // Act
        var status = service.GetStatus();

        // Assert
        status.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void IsEnabled_WhenSlashCommandsEnabled_ReturnsTrue()
    {
        // Arrange
        var options = new DiscordOptions
        {
            BotToken = "test-token",
            EnableSlashCommands = true
        };
        var service = CreateService(options);

        // Act & Assert
        service.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void IsEnabled_WhenSlashCommandsDisabled_ReturnsFalse()
    {
        // Arrange
        var options = new DiscordOptions
        {
            BotToken = "test-token",
            EnableSlashCommands = false
        };
        var service = CreateService(options);

        // Act & Assert
        service.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void RegisteredModuleCount_Initially_ReturnsZero()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        service.RegisteredModuleCount.Should().Be(0);
    }

    [Fact]
    public async Task SendMessageAsync_WhenNotConnected_ThrowsInvalidOperationException()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SendMessageAsync(123UL, "test message", CancellationToken.None));
    }

    [Fact]
    public async Task SendEmbedAsync_WhenNotConnected_ThrowsInvalidOperationException()
    {
        // Arrange
        var service = CreateService();
        var embed = new EmbedData { Title = "Test", Description = "Test" };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SendEmbedAsync(123UL, embed, CancellationToken.None));
    }

    [Fact]
    public async Task ReplyToMessageAsync_WhenNotConnected_ThrowsInvalidOperationException()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ReplyToMessageAsync(123UL, 456UL, "reply", CancellationToken.None));
    }

    [Fact]
    public async Task SendAndAwaitFirstResponseAsync_WhenNotConnected_ThrowsInvalidOperationException()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SendAndAwaitFirstResponseAsync(
                new[] { 123UL },
                "test",
                TimeSpan.FromSeconds(10),
                CancellationToken.None));
    }

    [Fact]
    public async Task SendEmbedAndAwaitFirstResponseAsync_WhenNotConnected_ThrowsInvalidOperationException()
    {
        // Arrange
        var service = CreateService();
        var embed = new EmbedData { Title = "Test", Description = "Test" };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SendEmbedAndAwaitFirstResponseAsync(
                new[] { 123UL },
                embed,
                TimeSpan.FromSeconds(10),
                CancellationToken.None));
    }

    [Fact]
    public async Task BroadcastWithResponseHandlerAsync_WhenNotConnected_ThrowsInvalidOperationException()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.BroadcastWithResponseHandlerAsync(
                new[] { 123UL },
                "test",
                _ => Task.FromResult(false),
                TimeSpan.FromSeconds(10),
                CancellationToken.None));
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert (should not throw)
        service.Dispose();
        service.Dispose();
    }

    [Fact]
    public void ImplementsIChatBotService()
    {
        // Arrange
        var service = CreateService();

        // Assert
        service.Should().BeAssignableTo<IChatBotService>();
    }

    [Fact]
    public void ImplementsIBotCommandService()
    {
        // Arrange
        var service = CreateService();

        // Assert
        service.Should().BeAssignableTo<IBotCommandService>();
    }

    [Fact]
    public void ImplementsIMessagingService()
    {
        // Arrange
        var service = CreateService();

        // Assert
        service.Should().BeAssignableTo<IMessagingService>();
    }

    public void Dispose()
    {
        _service?.Dispose();
    }
}
