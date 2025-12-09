using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Mystira.App.Application.CQRS.Discord.Commands;
using Mystira.App.Application.Ports.Messaging;

namespace Mystira.App.Application.Tests.CQRS.Discord;

/// <summary>
/// Unit tests for SendDiscordEmbedCommandHandler.
/// Tests embed messaging functionality via the IChatBotService interface.
/// </summary>
public class SendDiscordEmbedCommandHandlerTests
{
    private readonly Mock<IChatBotService> _mockChatBotService;
    private readonly Mock<ILogger<SendDiscordEmbedCommandHandler>> _mockLogger;
    private readonly SendDiscordEmbedCommandHandler _handler;

    public SendDiscordEmbedCommandHandlerTests()
    {
        _mockChatBotService = new Mock<IChatBotService>();
        _mockLogger = new Mock<ILogger<SendDiscordEmbedCommandHandler>>();
        _handler = new SendDiscordEmbedCommandHandler(_mockLogger.Object, _mockChatBotService.Object);
    }

    [Fact]
    public void Constructor_WhenChatBotServiceIsNull_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SendDiscordEmbedCommandHandler(_mockLogger.Object, null!));
    }

    [Fact]
    public async Task Handle_WhenBotIsNotConnected_ReturnsFailure()
    {
        // Arrange
        _mockChatBotService.Setup(x => x.IsConnected).Returns(false);
        var command = new SendDiscordEmbedCommand(
            ChannelId: 123456789UL,
            Title: "Test Title",
            Description: "Test Description",
            ColorRed: 255,
            ColorGreen: 0,
            ColorBlue: 0,
            Footer: null,
            Fields: null
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not connected");
        _mockChatBotService.Verify(
            x => x.SendEmbedAsync(It.IsAny<ulong>(), It.IsAny<EmbedData>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenBotIsConnected_SendsEmbedAndReturnsSuccess()
    {
        // Arrange
        _mockChatBotService.Setup(x => x.IsConnected).Returns(true);
        _mockChatBotService
            .Setup(x => x.SendEmbedAsync(It.IsAny<ulong>(), It.IsAny<EmbedData>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var command = new SendDiscordEmbedCommand(
            ChannelId: 123456789UL,
            Title: "Test Title",
            Description: "Test Description",
            ColorRed: 255,
            ColorGreen: 128,
            ColorBlue: 0,
            Footer: "Test Footer",
            Fields: null
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("successfully");
        _mockChatBotService.Verify(
            x => x.SendEmbedAsync(
                123456789UL,
                It.Is<EmbedData>(e =>
                    e.Title == "Test Title" &&
                    e.Description == "Test Description" &&
                    e.ColorRed == 255 &&
                    e.ColorGreen == 128 &&
                    e.ColorBlue == 0 &&
                    e.Footer == "Test Footer"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithFields_ConvertsFieldsCorrectly()
    {
        // Arrange
        _mockChatBotService.Setup(x => x.IsConnected).Returns(true);
        EmbedData? capturedEmbed = null;
        _mockChatBotService
            .Setup(x => x.SendEmbedAsync(It.IsAny<ulong>(), It.IsAny<EmbedData>(), It.IsAny<CancellationToken>()))
            .Callback<ulong, EmbedData, CancellationToken>((_, embed, _) => capturedEmbed = embed)
            .Returns(Task.CompletedTask);

        var fields = new List<DiscordEmbedField>
        {
            new("Field 1", "Value 1", true),
            new("Field 2", "Value 2", false)
        };

        var command = new SendDiscordEmbedCommand(
            ChannelId: 123456789UL,
            Title: "Test",
            Description: "Test",
            ColorRed: 0,
            ColorGreen: 0,
            ColorBlue: 255,
            Footer: null,
            Fields: fields
        );

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedEmbed.Should().NotBeNull();
        capturedEmbed!.Fields.Should().HaveCount(2);
        capturedEmbed.Fields![0].Name.Should().Be("Field 1");
        capturedEmbed.Fields[0].Value.Should().Be("Value 1");
        capturedEmbed.Fields[0].Inline.Should().BeTrue();
        capturedEmbed.Fields[1].Name.Should().Be("Field 2");
        capturedEmbed.Fields[1].Value.Should().Be("Value 2");
        capturedEmbed.Fields[1].Inline.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenSendEmbedThrows_ReturnsFailure()
    {
        // Arrange
        _mockChatBotService.Setup(x => x.IsConnected).Returns(true);
        _mockChatBotService
            .Setup(x => x.SendEmbedAsync(It.IsAny<ulong>(), It.IsAny<EmbedData>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Rate limited"));

        var command = new SendDiscordEmbedCommand(
            ChannelId: 123456789UL,
            Title: "Test",
            Description: "Test",
            ColorRed: 0,
            ColorGreen: 0,
            ColorBlue: 0,
            Footer: null,
            Fields: null
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Error sending embed");
        result.Message.Should().Contain("Rate limited");
    }

    [Fact]
    public async Task Handle_WithNullFields_SendsEmbedWithoutFields()
    {
        // Arrange
        _mockChatBotService.Setup(x => x.IsConnected).Returns(true);
        EmbedData? capturedEmbed = null;
        _mockChatBotService
            .Setup(x => x.SendEmbedAsync(It.IsAny<ulong>(), It.IsAny<EmbedData>(), It.IsAny<CancellationToken>()))
            .Callback<ulong, EmbedData, CancellationToken>((_, embed, _) => capturedEmbed = embed)
            .Returns(Task.CompletedTask);

        var command = new SendDiscordEmbedCommand(
            ChannelId: 123456789UL,
            Title: "Test",
            Description: "Test",
            ColorRed: 0,
            ColorGreen: 0,
            ColorBlue: 0,
            Footer: null,
            Fields: null
        );

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedEmbed.Should().NotBeNull();
        capturedEmbed!.Fields.Should().BeNull();
    }
}
