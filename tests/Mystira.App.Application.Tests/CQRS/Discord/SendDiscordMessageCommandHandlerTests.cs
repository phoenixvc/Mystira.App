using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Mystira.App.Application.CQRS.Discord.Commands;
using Mystira.App.Application.Ports.Messaging;

namespace Mystira.App.Application.Tests.CQRS.Discord;

/// <summary>
/// Unit tests for SendDiscordMessageCommandHandler.
/// Tests messaging functionality via the IChatBotService interface.
/// </summary>
public class SendDiscordMessageCommandHandlerTests
{
    private readonly Mock<IChatBotService> _mockChatBotService;
    private readonly Mock<ILogger<SendDiscordMessageCommandHandler>> _mockLogger;
    private readonly SendDiscordMessageCommandHandler _handler;

    public SendDiscordMessageCommandHandlerTests()
    {
        _mockChatBotService = new Mock<IChatBotService>();
        _mockLogger = new Mock<ILogger<SendDiscordMessageCommandHandler>>();
        _handler = new SendDiscordMessageCommandHandler(_mockLogger.Object, _mockChatBotService.Object);
    }

    [Fact]
    public void Constructor_WhenChatBotServiceIsNull_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SendDiscordMessageCommandHandler(_mockLogger.Object, null!));
    }

    [Fact]
    public async Task Handle_WhenBotIsNotConnected_ReturnsFailure()
    {
        // Arrange
        _mockChatBotService.Setup(x => x.IsConnected).Returns(false);
        var command = new SendDiscordMessageCommand(123456789UL, "Test message");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not connected");
        _mockChatBotService.Verify(x => x.SendMessageAsync(It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenBotIsConnected_SendsMessageAndReturnsSuccess()
    {
        // Arrange
        _mockChatBotService.Setup(x => x.IsConnected).Returns(true);
        _mockChatBotService
            .Setup(x => x.SendMessageAsync(It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var command = new SendDiscordMessageCommand(123456789UL, "Test message");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("successfully");
        _mockChatBotService.Verify(x => x.SendMessageAsync(123456789UL, "Test message", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenSendMessageThrows_ReturnsFailure()
    {
        // Arrange
        _mockChatBotService.Setup(x => x.IsConnected).Returns(true);
        _mockChatBotService
            .Setup(x => x.SendMessageAsync(It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Channel not found"));

        var command = new SendDiscordMessageCommand(123456789UL, "Test message");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Error sending message");
        result.Message.Should().Contain("Channel not found");
    }

    [Fact]
    public async Task Handle_PassesCancellationTokenToService()
    {
        // Arrange
        _mockChatBotService.Setup(x => x.IsConnected).Returns(true);
        _mockChatBotService
            .Setup(x => x.SendMessageAsync(It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var command = new SendDiscordMessageCommand(123456789UL, "Test message");
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        // Act
        await _handler.Handle(command, token);

        // Assert
        _mockChatBotService.Verify(x => x.SendMessageAsync(It.IsAny<ulong>(), It.IsAny<string>(), token), Times.Once);
    }
}
