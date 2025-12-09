using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Mystira.App.Application.Ports.Messaging;
using Mystira.App.Infrastructure.Teams.Configuration;
using Mystira.App.Infrastructure.Teams.Services;

namespace Mystira.App.Infrastructure.Teams.Tests;

public class TeamsBotServiceTests
{
    private readonly Mock<ILogger<TeamsBotService>> _loggerMock;
    private readonly Mock<IOptions<TeamsOptions>> _optionsMock;
    private readonly TeamsOptions _options;

    public TeamsBotServiceTests()
    {
        _loggerMock = new Mock<ILogger<TeamsBotService>>();
        _options = new TeamsOptions
        {
            Enabled = true,
            MicrosoftAppId = "test-app-id",
            MicrosoftAppPassword = "test-app-password",
            DefaultTimeoutSeconds = 30
        };
        _optionsMock = new Mock<IOptions<TeamsOptions>>();
        _optionsMock.Setup(o => o.Value).Returns(_options);
    }

    [Fact]
    public void Constructor_ShouldInitializeService()
    {
        // Act
        var service = new TeamsBotService(_optionsMock.Object, _loggerMock.Object);

        // Assert
        service.Should().NotBeNull();
        service.Platform.Should().Be(ChatPlatform.Teams);
        service.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task StartAsync_WhenEnabled_ShouldSetIsConnectedTrue()
    {
        // Arrange
        var service = new TeamsBotService(_optionsMock.Object, _loggerMock.Object);

        // Act
        await service.StartAsync();

        // Assert
        service.IsConnected.Should().BeTrue();
    }

    [Fact]
    public async Task StartAsync_WhenDisabled_ShouldNotConnect()
    {
        // Arrange
        _options.Enabled = false;
        var service = new TeamsBotService(_optionsMock.Object, _loggerMock.Object);

        // Act
        await service.StartAsync();

        // Assert
        service.IsConnected.Should().BeFalse();
    }

    [Fact]
    public async Task StartAsync_WhenMissingAppId_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _options.MicrosoftAppId = string.Empty;
        var service = new TeamsBotService(_optionsMock.Object, _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.StartAsync());
    }

    [Fact]
    public async Task StopAsync_ShouldSetIsConnectedFalse()
    {
        // Arrange
        var service = new TeamsBotService(_optionsMock.Object, _loggerMock.Object);
        await service.StartAsync();

        // Act
        await service.StopAsync();

        // Assert
        service.IsConnected.Should().BeFalse();
    }

    [Fact]
    public void GetStatus_ShouldReturnCorrectStatus()
    {
        // Arrange
        var service = new TeamsBotService(_optionsMock.Object, _loggerMock.Object);

        // Act
        var status = service.GetStatus();

        // Assert
        status.Should().NotBeNull();
        status.IsEnabled.Should().BeTrue();
        status.BotName.Should().Be("Teams Bot");
    }

    [Fact]
    public async Task SendMessageAsync_WhenNotConnected_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var service = new TeamsBotService(_optionsMock.Object, _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SendMessageAsync(123456, "test message"));
    }

    [Fact]
    public async Task SendMessageAsync_WhenNoConversationReference_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var service = new TeamsBotService(_optionsMock.Object, _loggerMock.Object);
        await service.StartAsync();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SendMessageAsync(123456, "test message"));

        exception.Message.Should().Contain("No conversation reference found");
    }

    [Fact]
    public void Dispose_ShouldClearConversations()
    {
        // Arrange
        var service = new TeamsBotService(_optionsMock.Object, _loggerMock.Object);

        // Act
        service.Dispose();

        // Assert - should not throw
        service.GetStatus().ServerCount.Should().Be(0);
    }
}
