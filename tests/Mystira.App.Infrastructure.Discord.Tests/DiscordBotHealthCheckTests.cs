using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Mystira.App.Infrastructure.Discord.Configuration;
using Mystira.App.Infrastructure.Discord.HealthChecks;
using Mystira.App.Infrastructure.Discord.Services;

namespace Mystira.App.Infrastructure.Discord.Tests;

/// <summary>
/// Unit tests for DiscordBotHealthCheck.
/// Verifies that health check correctly reports bot status.
/// </summary>
public class DiscordBotHealthCheckTests : IDisposable
{
    private readonly Mock<ILogger<DiscordBotService>> _mockLogger;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private DiscordBotService? _service;

    public DiscordBotHealthCheckTests()
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
    public async Task CheckHealthAsync_WhenBotNotConnected_ReturnsUnhealthy()
    {
        // Arrange
        var service = CreateService();
        var healthCheck = new DiscordBotHealthCheck(service);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("not connected");
        result.Data.Should().ContainKey("IsConnected");
        result.Data["IsConnected"].Should().Be(false);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenTokenNotConfigured_ReturnsUnhealthy()
    {
        // Arrange
        var options = new DiscordOptions { BotToken = "" };
        var service = CreateService(options);
        var healthCheck = new DiscordBotHealthCheck(service);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Data.Should().ContainKey("IsEnabled");
        result.Data["IsEnabled"].Should().Be(false);
    }

    [Fact]
    public void Constructor_AcceptsConcreteDiscordBotService()
    {
        // Arrange
        var service = CreateService();

        // Act
        var healthCheck = new DiscordBotHealthCheck(service);

        // Assert - if we get here without exception, the concrete type is accepted
        healthCheck.Should().NotBeNull();
    }

    [Fact]
    public async Task CheckHealthAsync_IncludesIsEnabledInData()
    {
        // Arrange
        var service = CreateService();
        var healthCheck = new DiscordBotHealthCheck(service);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Data.Should().ContainKey("IsEnabled");
    }

    public void Dispose()
    {
        _service?.Dispose();
    }
}
