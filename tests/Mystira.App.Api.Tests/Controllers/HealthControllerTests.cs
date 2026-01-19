using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Mystira.App.Api.Controllers;
using Mystira.App.Api.Models;
using Mystira.App.Application.CQRS.Health.Queries;
using Wolverine;
using Xunit;

namespace Mystira.App.Api.Tests.Controllers;

public class HealthControllerTests
{
    private readonly Mock<IMessageBus> _mockBus;
    private readonly Mock<ILogger<HealthController>> _mockLogger;
    private readonly HealthController _controller;

    public HealthControllerTests()
    {
        _mockBus = new Mock<IMessageBus>();
        _mockLogger = new Mock<ILogger<HealthController>>();
        _controller = new HealthController(_mockBus.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetHealth_WhenHealthy_ReturnsOkWithHealthyStatus()
    {
        // Arrange
        var healthCheckResult = new HealthCheckResult(
            Status: "Healthy",
            Duration: TimeSpan.FromMilliseconds(100),
            Results: new Dictionary<string, object>()
        );

        _mockBus
            .Setup(x => x.InvokeAsync<HealthCheckResult>(It.IsAny<GetHealthCheckQuery>(), It.IsAny<CancellationToken>(), It.IsAny<TimeSpan?>()))
            .ReturnsAsync(healthCheckResult);

        // Act
        var result = await _controller.GetHealth();

        // Assert
        result.Should().NotBeNull();
        var objResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        objResult.StatusCode.Should().Be(200);

        var response = objResult.Value.Should().BeOfType<HealthCheckResponse>().Subject;
        response.Status.Should().Be("Healthy");
    }

    [Fact]
    public async Task GetHealth_WhenUnhealthy_ReturnsServiceUnavailableWithUnhealthyStatus()
    {
        // Arrange
        var healthCheckResult = new HealthCheckResult(
            Status: "Unhealthy",
            Duration: TimeSpan.FromMilliseconds(100),
            Results: new Dictionary<string, object>
            {
                ["database"] = new { Status = "Unhealthy", Description = "Database connection failed" }
            }
        );

        _mockBus
            .Setup(x => x.InvokeAsync<HealthCheckResult>(It.IsAny<GetHealthCheckQuery>(), It.IsAny<CancellationToken>(), It.IsAny<TimeSpan?>()))
            .ReturnsAsync(healthCheckResult);

        // Act
        var result = await _controller.GetHealth();

        // Assert
        result.Should().NotBeNull();
        var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(503); // Service Unavailable

        var response = objectResult.Value.Should().BeOfType<HealthCheckResponse>().Subject;
        response.Status.Should().Be("Unhealthy");
    }

    [Fact]
    public async Task GetHealth_WhenDegraded_ReturnsOkWithDegradedStatus()
    {
        // Arrange
        var healthCheckResult = new HealthCheckResult(
            Status: "Degraded",
            Duration: TimeSpan.FromMilliseconds(100),
            Results: new Dictionary<string, object>
            {
                ["cache"] = new { Status = "Degraded", Description = "Cache running slowly" }
            }
        );

        _mockBus
            .Setup(x => x.InvokeAsync<HealthCheckResult>(It.IsAny<GetHealthCheckQuery>(), It.IsAny<CancellationToken>(), It.IsAny<TimeSpan?>()))
            .ReturnsAsync(healthCheckResult);

        // Act
        var result = await _controller.GetHealth();

        // Assert
        result.Should().NotBeNull();
        var objResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        objResult.StatusCode.Should().Be(200);

        var response = objResult.Value.Should().BeOfType<HealthCheckResponse>().Subject;
        response.Status.Should().Be("Degraded");
    }

    [Fact]
    public async Task GetHealth_IncludesResponseTime()
    {
        // Arrange
        var healthCheckResult = new HealthCheckResult(
            Status: "Healthy",
            Duration: TimeSpan.FromMilliseconds(150),
            Results: new Dictionary<string, object>()
        );

        _mockBus
            .Setup(x => x.InvokeAsync<HealthCheckResult>(It.IsAny<GetHealthCheckQuery>(), It.IsAny<CancellationToken>(), It.IsAny<TimeSpan?>()))
            .ReturnsAsync(healthCheckResult);

        // Act
        var result = await _controller.GetHealth();

        // Assert
        var objResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        var response = objResult.Value.Should().BeOfType<HealthCheckResponse>().Subject;

        response.Duration.Should().Be(TimeSpan.FromMilliseconds(150));
        response.Status.Should().Be("Healthy");
    }

    [Fact]
    public async Task GetHealth_IncludesChecksInformation()
    {
        // Arrange
        var healthCheckResult = new HealthCheckResult(
            Status: "Healthy",
            Duration: TimeSpan.FromMilliseconds(100),
            Results: new Dictionary<string, object>
            {
                ["database"] = new { Status = "Healthy", Description = "Database connection successful" },
                ["storage"] = new { Status = "Healthy", Description = "Storage service available" }
            }
        );

        _mockBus
            .Setup(x => x.InvokeAsync<HealthCheckResult>(It.IsAny<GetHealthCheckQuery>(), It.IsAny<CancellationToken>(), It.IsAny<TimeSpan?>()))
            .ReturnsAsync(healthCheckResult);

        // Act
        var result = await _controller.GetHealth();

        // Assert
        var objResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        var response = objResult.Value.Should().BeOfType<HealthCheckResponse>().Subject;

        response.Results.Should().HaveCount(2);
        response.Results.Should().ContainKey("database");
        response.Results.Should().ContainKey("storage");

        response.Status.Should().Be("Healthy");
        response.Duration.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task GetReady_ReturnsOkWithStatus()
    {
        // Arrange
        var readinessResult = new ProbeResult("Ready", DateTime.UtcNow);

        _mockBus
            .Setup(x => x.InvokeAsync<ProbeResult>(It.IsAny<GetReadinessQuery>(), It.IsAny<CancellationToken>(), It.IsAny<TimeSpan?>()))
            .ReturnsAsync(readinessResult);

        // Act
        var result = await _controller.GetReady();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetLive_ReturnsOkWithStatus()
    {
        // Arrange
        var livenessResult = new ProbeResult("Alive", DateTime.UtcNow);

        _mockBus
            .Setup(x => x.InvokeAsync<ProbeResult>(It.IsAny<GetLivenessQuery>(), It.IsAny<CancellationToken>(), It.IsAny<TimeSpan?>()))
            .ReturnsAsync(livenessResult);

        // Act
        var result = await _controller.GetLive();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<OkObjectResult>();
    }
}
