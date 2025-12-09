using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Mystira.App.Application.Ports.Messaging;
using Mystira.App.Infrastructure.WhatsApp.Configuration;
using Mystira.App.Infrastructure.WhatsApp.Services;

namespace Mystira.App.Infrastructure.WhatsApp.Tests;

public class WhatsAppBotServiceTests
{
    private readonly Mock<ILogger<WhatsAppBotService>> _loggerMock;
    private readonly Mock<IOptions<WhatsAppOptions>> _optionsMock;
    private readonly WhatsAppOptions _options;

    public WhatsAppBotServiceTests()
    {
        _loggerMock = new Mock<ILogger<WhatsAppBotService>>();
        _options = new WhatsAppOptions
        {
            Enabled = true,
            ConnectionString = "endpoint=https://test.communication.azure.com/;accesskey=test",
            ChannelRegistrationId = "test-channel-id",
            MaxRetryAttempts = 3,
            DefaultTimeoutSeconds = 30
        };
        _optionsMock = new Mock<IOptions<WhatsAppOptions>>();
        _optionsMock.Setup(o => o.Value).Returns(_options);
    }

    [Fact]
    public void Constructor_ShouldInitializeService()
    {
        // Act
        var service = new WhatsAppBotService(_optionsMock.Object, _loggerMock.Object);

        // Assert
        service.Should().NotBeNull();
        service.Platform.Should().Be(ChatPlatform.WhatsApp);
        service.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task StartAsync_WhenDisabled_ShouldNotConnect()
    {
        // Arrange
        _options.Enabled = false;
        var service = new WhatsAppBotService(_optionsMock.Object, _loggerMock.Object);

        // Act
        await service.StartAsync();

        // Assert
        service.IsConnected.Should().BeFalse();
    }

    [Fact]
    public async Task StartAsync_WhenMissingConnectionString_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _options.ConnectionString = string.Empty;
        var service = new WhatsAppBotService(_optionsMock.Object, _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.StartAsync());
    }

    [Fact]
    public async Task StartAsync_WhenMissingChannelRegistrationId_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _options.ChannelRegistrationId = string.Empty;
        var service = new WhatsAppBotService(_optionsMock.Object, _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.StartAsync());
    }

    [Fact]
    public async Task StopAsync_ShouldSetIsConnectedFalse()
    {
        // Arrange - note: StartAsync will fail without valid connection string,
        // so we just test that StopAsync doesn't throw
        var service = new WhatsAppBotService(_optionsMock.Object, _loggerMock.Object);

        // Act
        await service.StopAsync();

        // Assert
        service.IsConnected.Should().BeFalse();
    }

    [Fact]
    public void GetStatus_ShouldReturnCorrectStatus()
    {
        // Arrange
        var service = new WhatsAppBotService(_optionsMock.Object, _loggerMock.Object);

        // Act
        var status = service.GetStatus();

        // Assert
        status.Should().NotBeNull();
        status.IsEnabled.Should().BeTrue();
        status.BotName.Should().Be("WhatsApp Bot");
    }

    [Theory]
    [InlineData("+1234567890", 1234567890ul)]
    [InlineData("1234567890", 1234567890ul)]
    [InlineData("+1-234-567-890", 1234567890ul)]
    [InlineData("+1 234 567 890", 1234567890ul)]
    public void GetChannelIdFromPhoneNumber_ShouldNormalizeAndReturnNumericId(string phoneNumber, ulong expected)
    {
        // Act
        var result = WhatsAppBotService.GetChannelIdFromPhoneNumber(phoneNumber);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void GetChannelIdFromPhoneNumber_WithNonNumericInput_ShouldReturnHashedId()
    {
        // Arrange
        var phoneNumber = "invalid-phone";

        // Act
        var result = WhatsAppBotService.GetChannelIdFromPhoneNumber(phoneNumber);

        // Assert - should return a stable hash, not zero
        result.Should().NotBe(0ul);

        // Should be deterministic
        var result2 = WhatsAppBotService.GetChannelIdFromPhoneNumber(phoneNumber);
        result.Should().Be(result2);
    }

    [Fact]
    public void RegisterConversation_ShouldReturnConsistentChannelId()
    {
        // Arrange
        var service = new WhatsAppBotService(_optionsMock.Object, _loggerMock.Object);
        var phoneNumber = "+1234567890";

        // Act
        var channelId1 = service.RegisterConversation(phoneNumber);
        var channelId2 = service.RegisterConversation(phoneNumber);

        // Assert
        channelId1.Should().Be(channelId2);
    }

    [Fact]
    public async Task SendMessageAsync_WhenNotConnected_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var service = new WhatsAppBotService(_optionsMock.Object, _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SendMessageAsync(123456, "test message"));
    }

    [Fact]
    public void Dispose_ShouldClearConversations()
    {
        // Arrange
        var service = new WhatsAppBotService(_optionsMock.Object, _loggerMock.Object);
        service.RegisterConversation("+1234567890");

        // Act
        service.Dispose();

        // Assert - should not throw
        service.GetStatus().ServerCount.Should().Be(0);
    }
}
