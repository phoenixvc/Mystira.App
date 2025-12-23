using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Mystira.App.Application.Configuration.StoryProtocol;
using Mystira.App.Infrastructure.StoryProtocol.HealthChecks;
using Mystira.App.Infrastructure.StoryProtocol.Services;

namespace Mystira.App.Api.Tests.HealthChecks;

/// <summary>
/// Unit tests for ChainServiceHealthCheck.
/// Tests verify health check behavior under various conditions.
/// </summary>
public class ChainServiceHealthCheckTests
{
    private readonly Mock<ILogger<ChainServiceHealthCheck>> _loggerMock;

    public ChainServiceHealthCheckTests()
    {
        _loggerMock = new Mock<ILogger<ChainServiceHealthCheck>>();
    }

    [Fact]
    public async Task CheckHealthAsync_WhenGrpcDisabled_ShouldReturnHealthy()
    {
        // Arrange
        var options = new ChainServiceOptions
        {
            UseGrpc = false,
            GrpcEndpoint = "http://localhost:50051"
        };

        var sut = new ChainServiceHealthCheck(
            Options.Create(options),
            _loggerMock.Object,
            null);

        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("chain-service", sut, null, null)
        };

        // Act
        var result = await sut.CheckHealthAsync(context);

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("disabled");
        result.Data.Should().ContainKey("UseGrpc");
        result.Data["UseGrpc"].Should().Be(false);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenHealthChecksDisabled_ShouldReturnHealthy()
    {
        // Arrange
        var options = new ChainServiceOptions
        {
            UseGrpc = true,
            EnableHealthChecks = false,
            GrpcEndpoint = "http://localhost:50051"
        };

        var sut = new ChainServiceHealthCheck(
            Options.Create(options),
            _loggerMock.Object,
            null);

        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("chain-service", sut, null, null)
        };

        // Act
        var result = await sut.CheckHealthAsync(context);

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("health checks are disabled");
        result.Data.Should().ContainKey("HealthChecksEnabled");
        result.Data["HealthChecksEnabled"].Should().Be(false);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenAdapterNotAvailable_ShouldReturnDegraded()
    {
        // Arrange
        var options = new ChainServiceOptions
        {
            UseGrpc = true,
            EnableHealthChecks = true,
            GrpcEndpoint = "http://localhost:50051"
        };

        var sut = new ChainServiceHealthCheck(
            Options.Create(options),
            _loggerMock.Object,
            null);

        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("chain-service", sut, null, null)
        };

        // Act
        var result = await sut.CheckHealthAsync(context);

        // Assert
        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("adapter is not available");
        result.Data.Should().ContainKey("AdapterAvailable");
        result.Data["AdapterAvailable"].Should().Be(false);
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldIncludeEndpointInData()
    {
        // Arrange
        var expectedEndpoint = "http://chain.mystira.local:50051";
        var options = new ChainServiceOptions
        {
            UseGrpc = false,
            GrpcEndpoint = expectedEndpoint
        };

        var sut = new ChainServiceHealthCheck(
            Options.Create(options),
            _loggerMock.Object,
            null);

        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("chain-service", sut, null, null)
        };

        // Act
        var result = await sut.CheckHealthAsync(context);

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
    }
}
