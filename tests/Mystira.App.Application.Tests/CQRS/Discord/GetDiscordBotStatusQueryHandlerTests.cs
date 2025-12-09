using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Mystira.App.Application.CQRS.Discord.Queries;
using Mystira.App.Application.Ports.Messaging;

namespace Mystira.App.Application.Tests.CQRS.Discord;

/// <summary>
/// Unit tests for GetDiscordBotStatusQueryHandler.
/// Tests status retrieval functionality via the IChatBotService interface.
/// </summary>
public class GetDiscordBotStatusQueryHandlerTests
{
    private readonly Mock<IChatBotService> _mockChatBotService;
    private readonly Mock<ILogger<GetDiscordBotStatusQueryHandler>> _mockLogger;
    private readonly GetDiscordBotStatusQueryHandler _handler;

    public GetDiscordBotStatusQueryHandlerTests()
    {
        _mockChatBotService = new Mock<IChatBotService>();
        _mockLogger = new Mock<ILogger<GetDiscordBotStatusQueryHandler>>();
        _handler = new GetDiscordBotStatusQueryHandler(_mockLogger.Object, _mockChatBotService.Object);
    }

    [Fact]
    public void Constructor_WhenChatBotServiceIsNull_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new GetDiscordBotStatusQueryHandler(_mockLogger.Object, null!));
    }

    [Fact]
    public async Task Handle_WhenBotIsConnected_ReturnsConnectedStatus()
    {
        // Arrange
        var botStatus = new BotStatus
        {
            IsEnabled = true,
            IsConnected = true,
            BotName = "TestBot",
            BotId = 123456789012345678UL,
            ServerCount = 5
        };
        _mockChatBotService.Setup(x => x.GetStatus()).Returns(botStatus);

        var query = new GetDiscordBotStatusQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Enabled.Should().BeTrue();
        result.Connected.Should().BeTrue();
        result.BotUsername.Should().Be("TestBot");
        result.BotId.Should().Be(123456789012345678UL);
        result.Message.Should().Contain("connected");
    }

    [Fact]
    public async Task Handle_WhenBotIsNotConnected_ReturnsNotConnectedStatus()
    {
        // Arrange
        var botStatus = new BotStatus
        {
            IsEnabled = true,
            IsConnected = false,
            BotName = null,
            BotId = null,
            ServerCount = 0
        };
        _mockChatBotService.Setup(x => x.GetStatus()).Returns(botStatus);

        var query = new GetDiscordBotStatusQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Enabled.Should().BeTrue();
        result.Connected.Should().BeFalse();
        result.BotUsername.Should().BeNull();
        result.BotId.Should().BeNull();
        result.Message.Should().Contain("not connected");
    }

    [Fact]
    public async Task Handle_WhenBotIsDisabled_ReturnsDisabledStatus()
    {
        // Arrange
        var botStatus = new BotStatus
        {
            IsEnabled = false,
            IsConnected = false,
            BotName = null,
            BotId = null,
            ServerCount = 0
        };
        _mockChatBotService.Setup(x => x.GetStatus()).Returns(botStatus);

        var query = new GetDiscordBotStatusQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Enabled.Should().BeFalse();
        result.Connected.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ReturnsCorrectBotId()
    {
        // Arrange
        var expectedBotId = 987654321098765432UL;
        var botStatus = new BotStatus
        {
            IsEnabled = true,
            IsConnected = true,
            BotName = "TestBot",
            BotId = expectedBotId,
            ServerCount = 10
        };
        _mockChatBotService.Setup(x => x.GetStatus()).Returns(botStatus);

        var query = new GetDiscordBotStatusQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.BotId.Should().Be(expectedBotId);
    }

    [Fact]
    public async Task Handle_WhenBotIdIsNull_ReturnsNullBotId()
    {
        // Arrange
        var botStatus = new BotStatus
        {
            IsEnabled = true,
            IsConnected = false,
            BotName = null,
            BotId = null,
            ServerCount = 0
        };
        _mockChatBotService.Setup(x => x.GetStatus()).Returns(botStatus);

        var query = new GetDiscordBotStatusQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.BotId.Should().BeNull();
    }
}
