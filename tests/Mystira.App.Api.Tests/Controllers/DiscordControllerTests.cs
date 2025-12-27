using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Mystira.App.Api.Controllers;
using Mystira.App.Application.CQRS.Discord.Commands;
using Mystira.App.Application.CQRS.Discord.Queries;
using Wolverine;
using Xunit;

namespace Mystira.App.Api.Tests.Controllers;

public class DiscordControllerTests
{
    private readonly Mock<IMessageBus> _mockBus;
    private readonly Mock<ILogger<DiscordController>> _mockLogger;
    private readonly DiscordController _controller;

    public DiscordControllerTests()
    {
        _mockBus = new Mock<IMessageBus>();
        _mockLogger = new Mock<ILogger<DiscordController>>();
        _controller = new DiscordController(_mockBus.Object, _mockLogger.Object);

        // Setup HttpContext for TraceIdentifier
        var httpContext = new DefaultHttpContext();
        httpContext.TraceIdentifier = "test-trace-id";
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    #region GetStatus Tests

    [Fact]
    public async Task GetStatus_WhenBotEnabled_ReturnsOkWithStatus()
    {
        // Arrange
        var status = new DiscordBotStatusResponse(
            Enabled: true,
            Connected: true,
            BotUsername: "TestBot",
            BotId: 123456789UL,
            Message: "Bot is running"
        );

        _mockBus
            .Setup(x => x.InvokeAsync<DiscordBotStatusResponse>(
                It.IsAny<GetDiscordBotStatusQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(status);

        // Act
        var result = await _controller.GetStatus();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetStatus_WhenBotDisabled_ReturnsOkWithDisabledStatus()
    {
        // Arrange
        var status = new DiscordBotStatusResponse(
            Enabled: false,
            Connected: false,
            BotUsername: null,
            BotId: null,
            Message: "Bot is disabled"
        );

        _mockBus
            .Setup(x => x.InvokeAsync<DiscordBotStatusResponse>(
                It.IsAny<GetDiscordBotStatusQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(status);

        // Act
        var result = await _controller.GetStatus();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    #endregion

    #region SendMessage Tests

    [Fact]
    public async Task SendMessage_WhenSuccessful_ReturnsOk()
    {
        // Arrange
        var request = new SendMessageRequest
        {
            ChannelId = 123456789UL,
            Message = "Hello, Discord!"
        };

        _mockBus
            .Setup(x => x.InvokeAsync<(bool, string)>(
                It.IsAny<SendDiscordMessageCommand>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync((true, "Message sent successfully"));

        // Act
        var result = await _controller.SendMessage(request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task SendMessage_WhenBotNotEnabled_ReturnsBadRequest()
    {
        // Arrange
        var request = new SendMessageRequest
        {
            ChannelId = 123456789UL,
            Message = "Hello, Discord!"
        };

        _mockBus
            .Setup(x => x.InvokeAsync<(bool, string)>(
                It.IsAny<SendDiscordMessageCommand>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync((false, "Discord bot is not enabled"));

        // Act
        var result = await _controller.SendMessage(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task SendMessage_WhenBotNotConnected_ReturnsServiceUnavailable()
    {
        // Arrange
        var request = new SendMessageRequest
        {
            ChannelId = 123456789UL,
            Message = "Hello, Discord!"
        };

        _mockBus
            .Setup(x => x.InvokeAsync<(bool, string)>(
                It.IsAny<SendDiscordMessageCommand>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync((false, "Discord bot is not connected"));

        // Act
        var result = await _controller.SendMessage(request);

        // Assert
        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(503);
    }

    [Fact]
    public async Task SendMessage_WhenOtherError_ReturnsInternalServerError()
    {
        // Arrange
        var request = new SendMessageRequest
        {
            ChannelId = 123456789UL,
            Message = "Hello, Discord!"
        };

        _mockBus
            .Setup(x => x.InvokeAsync<(bool, string)>(
                It.IsAny<SendDiscordMessageCommand>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync((false, "Unknown error occurred"));

        // Act
        var result = await _controller.SendMessage(request);

        // Assert
        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region SendEmbed Tests

    [Fact]
    public async Task SendEmbed_WhenSuccessful_ReturnsOk()
    {
        // Arrange
        var request = new SendEmbedRequest
        {
            ChannelId = 123456789UL,
            Title = "Test Embed",
            Description = "This is a test embed",
            ColorRed = 255,
            ColorGreen = 128,
            ColorBlue = 0,
            Footer = "Test Footer",
            Fields = new List<EmbedField>
            {
                new EmbedField { Name = "Field 1", Value = "Value 1", Inline = true }
            }
        };

        _mockBus
            .Setup(x => x.InvokeAsync<(bool, string)>(
                It.IsAny<SendDiscordEmbedCommand>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync((true, "Embed sent successfully"));

        // Act
        var result = await _controller.SendEmbed(request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task SendEmbed_WhenBotNotEnabled_ReturnsBadRequest()
    {
        // Arrange
        var request = new SendEmbedRequest
        {
            ChannelId = 123456789UL,
            Title = "Test Embed",
            Description = "This is a test embed"
        };

        _mockBus
            .Setup(x => x.InvokeAsync<(bool, string)>(
                It.IsAny<SendDiscordEmbedCommand>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync((false, "Discord bot is not enabled"));

        // Act
        var result = await _controller.SendEmbed(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task SendEmbed_WhenBotNotConnected_ReturnsServiceUnavailable()
    {
        // Arrange
        var request = new SendEmbedRequest
        {
            ChannelId = 123456789UL,
            Title = "Test Embed",
            Description = "This is a test embed"
        };

        _mockBus
            .Setup(x => x.InvokeAsync<(bool, string)>(
                It.IsAny<SendDiscordEmbedCommand>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync((false, "Discord bot is not connected"));

        // Act
        var result = await _controller.SendEmbed(request);

        // Assert
        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(503);
    }

    [Fact]
    public async Task SendEmbed_WithoutFields_StillSucceeds()
    {
        // Arrange
        var request = new SendEmbedRequest
        {
            ChannelId = 123456789UL,
            Title = "Test Embed",
            Description = "This is a test embed without fields",
            Fields = null
        };

        _mockBus
            .Setup(x => x.InvokeAsync<(bool, string)>(
                It.IsAny<SendDiscordEmbedCommand>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync((true, "Embed sent successfully"));

        // Act
        var result = await _controller.SendEmbed(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    #endregion
}
