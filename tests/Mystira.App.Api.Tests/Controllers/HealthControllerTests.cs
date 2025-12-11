using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Mystira.App.Api.Controllers;
using Mystira.App.Application.CQRS.Health.Queries;
using Mystira.App.Contracts.Responses.Common;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Mystira.App.Api.Tests.Controllers;

public class HealthControllerTests
{
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<ILogger<HealthController>> _mockLogger;
    private readonly HealthController _controller;

    public HealthControllerTests()
    {
        _mockMediator = new Mock<IMediator>();
        _mockLogger = new Mock<ILogger<HealthController>>();
        _controller = new HealthController(_mockMediator.Object, _mockLogger.Object);
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

        _mockMediator
            .Setup(x => x.Send(It.IsAny<GetHealthCheckQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(healthCheckResult);

        // Act
        var result = await _controller.GetHealth();

        // Assert
        result.Should().NotBeNull();
        result.Result.Should().BeOfType<ObjectResult>();
        
        var objResult = result.Result as ObjectResult;
        objResult!.StatusCode.Should().Be(200);
        objResult.Value.Should().BeOfType<HealthCheckResponse>();
        
        var response = objResult.Value as HealthCheckResponse;
        response!.Status.Should().Be("Healthy");
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

        _mockMediator
            .Setup(x => x.Send(It.IsAny<GetHealthCheckQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(healthCheckResult);

        // Act
        var result = await _controller.GetHealth();

        // Assert
        result.Should().NotBeNull();
        result.Result.Should().BeOfType<ObjectResult>();
        
        var objectResult = result.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(503); // Service Unavailable
        
        objectResult.Value.Should().BeOfType<HealthCheckResponse>();
        var response = objectResult.Value as HealthCheckResponse;
        response!.Status.Should().Be("Unhealthy");
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

        _mockMediator
            .Setup(x => x.Send(It.IsAny<GetHealthCheckQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(healthCheckResult);

        // Act
        var result = await _controller.GetHealth();

        // Assert
        result.Should().NotBeNull();
        result.Result.Should().BeOfType<ObjectResult>();
        
        var objResult = result.Result as ObjectResult;
        objResult!.StatusCode.Should().Be(200);
        objResult.Value.Should().BeOfType<HealthCheckResponse>();
        
        var response = objResult.Value as HealthCheckResponse;
        response!.Status.Should().Be("Degraded");
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

        _mockMediator
            .Setup(x => x.Send(It.IsAny<GetHealthCheckQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(healthCheckResult);

        // Act
        var result = await _controller.GetHealth();

        // Assert
        var objResult = result.Result as ObjectResult;
        objResult.Should().NotBeNull();
        
        var response = objResult!.Value as HealthCheckResponse;
        response!.Duration.Should().Be(TimeSpan.FromMilliseconds(150));
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

        _mockMediator
            .Setup(x => x.Send(It.IsAny<GetHealthCheckQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(healthCheckResult);

        // Act
        var result = await _controller.GetHealth();

        // Assert
        var objResult = result.Result as ObjectResult;
        objResult.Should().NotBeNull();

        var response = objResult!.Value as HealthCheckResponse;
        response.Should().NotBeNull();

        response!.Results.Should().HaveCount(2);
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

        _mockMediator
            .Setup(x => x.Send(It.IsAny<GetReadinessQuery>(), It.IsAny<CancellationToken>()))
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

        _mockMediator
            .Setup(x => x.Send(It.IsAny<GetLivenessQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(livenessResult);

        // Act
        var result = await _controller.GetLive();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<OkObjectResult>();
    }
}
