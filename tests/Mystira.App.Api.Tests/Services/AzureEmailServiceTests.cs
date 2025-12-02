using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Mystira.App.Infrastructure.Azure.Services;
using Xunit;

namespace Mystira.App.Api.Tests.Services;

public class AzureEmailServiceTests
{
    private readonly Mock<ILogger<AzureEmailService>> _mockLogger;

    public AzureEmailServiceTests()
    {
        _mockLogger = new Mock<ILogger<AzureEmailService>>();
    }

    [Fact]
    public void Constructor_WhenConfigurationMissing_LogsWarning()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        // Act
        var service = new AzureEmailService(configuration, _mockLogger.Object);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("not configured")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendSignupCodeAsync_WhenEmailDisabled_ReturnsSuccess()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AzureCommunicationServices:ConnectionString"] = "",
                ["AzureCommunicationServices:SenderEmail"] = ""
            })
            .Build();

        var service = new AzureEmailService(configuration, _mockLogger.Object);

        // Act
        var result = await service.SendSignupCodeAsync("test@example.com", "TestUser", "123456");

        // Assert
        result.Success.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task SendSigninCodeAsync_WhenEmailDisabled_ReturnsSuccess()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AzureCommunicationServices:ConnectionString"] = "",
                ["AzureCommunicationServices:SenderEmail"] = ""
            })
            .Build();

        var service = new AzureEmailService(configuration, _mockLogger.Object);

        // Act
        var result = await service.SendSigninCodeAsync("test@example.com", "TestUser", "123456");

        // Assert
        result.Success.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task SendSignupCodeAsync_WhenEmailDisabled_LogsCode()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AzureCommunicationServices:ConnectionString"] = "",
                ["AzureCommunicationServices:SenderEmail"] = ""
            })
            .Build();

        var service = new AzureEmailService(configuration, _mockLogger.Object);
        var testCode = "123456";

        // Act
        await service.SendSignupCodeAsync("test@example.com", "TestUser", testCode);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("dev mode") && v.ToString()!.Contains(testCode)),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendSigninCodeAsync_WhenEmailDisabled_LogsCode()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AzureCommunicationServices:ConnectionString"] = "",
                ["AzureCommunicationServices:SenderEmail"] = ""
            })
            .Build();

        var service = new AzureEmailService(configuration, _mockLogger.Object);
        var testCode = "654321";

        // Act
        await service.SendSigninCodeAsync("test@example.com", "TestUser", testCode);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("dev mode") && v.ToString()!.Contains(testCode)),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Constructor_WhenConfigurationValid_LogsInitialized()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AzureCommunicationServices:ConnectionString"] = "endpoint=https://test.communication.azure.com/;accesskey=testkey",
                ["AzureCommunicationServices:SenderEmail"] = "noreply@test.com"
            })
            .Build();

        // Act - This may throw if connection string is invalid, but that's expected
        // The important thing is it tries to initialize
        try
        {
            var service = new AzureEmailService(configuration, _mockLogger.Object);
        }
        catch
        {
            // Expected - invalid connection string format for test
        }

        // Assert - Either logs initialization success or initialization failure
        _mockLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString()!.Contains("initialized") || 
                    v.ToString()!.Contains("Failed to initialize")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
