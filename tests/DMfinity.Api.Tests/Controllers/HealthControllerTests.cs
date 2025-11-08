using DMfinity.Api.Controllers;
using DMfinity.Api.Models;
using DMfinity.Api.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DMfinity.Api.Tests.Controllers;

public class HealthControllerTests
{
    private readonly Mock<IHealthCheckService> _mockHealthCheckService;
    private readonly Mock<ILogger<HealthController>> _mockLogger;
    private readonly HealthController _controller;

    public HealthControllerTests()
    {
        _mockHealthCheckService = new Mock<IHealthCheckService>();
        _mockLogger = new Mock<ILogger<HealthController>>();
        _controller = new HealthController(_mockHealthCheckService.Object, _mockLogger.Object);
    }


    [Fact]
    public async Task GetHealth_WhenHealthy_ReturnsOkWithHealthyStatus()
    {
        // Arrange
        var healthReport = new HealthReport(
            new Dictionary<string, HealthReportEntry>(),
            HealthStatus.Healthy,
            TimeSpan.FromMilliseconds(100));

        _mockHealthCheckService
            .Setup(x => x.CheckHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(healthReport);

        // Act
        var result = await _controller.GetHealth();

        // Assert
        result.Should().NotBeNull();
        result.Result.Should().BeOfType<ObjectResult>();
        
        var objResult = result.Result as ObjectResult;
        objResult!.Value.Should().BeOfType<HealthCheckResponse>();
        
        var response = objResult.Value as HealthCheckResponse;
        response!.Status.Should().Be("Healthy");
    }

    [Fact]
    public async Task GetHealth_WhenUnhealthy_ReturnsServiceUnavailableWithUnhealthyStatus()
    {
        // Arrange
        var healthReport = new HealthReport(
            new Dictionary<string, HealthReportEntry>
            {
                ["test"] = new HealthReportEntry(
                    HealthStatus.Unhealthy,
                    "Test failure",
                    TimeSpan.FromMilliseconds(50),
                    new Exception("Test exception"),
                    new Dictionary<string, object>())
            },
            HealthStatus.Unhealthy,
            TimeSpan.FromMilliseconds(100));

        _mockHealthCheckService
            .Setup(x => x.CheckHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(healthReport);

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
        var healthReport = new HealthReport(
            new Dictionary<string, HealthReportEntry>
            {
                ["test"] = new HealthReportEntry(
                    HealthStatus.Degraded,
                    "Test degraded",
                    TimeSpan.FromMilliseconds(50),
                    null,
                    new Dictionary<string, object>())
            },
            HealthStatus.Degraded,
            TimeSpan.FromMilliseconds(100));

        _mockHealthCheckService
            .Setup(x => x.CheckHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(healthReport);

        // Act
        var result = await _controller.GetHealth();

        // Assert
        result.Should().NotBeNull();
        result.Result.Should().BeOfType<ObjectResult>();
        
        var objResult = result.Result as ObjectResult;
        objResult.Should().NotBeNull("response should not be null");
        objResult!.Value.Should().BeOfType<HealthCheckResponse>();
        
        var response = objResult.Value as HealthCheckResponse;
        response!.Status.Should().Be("Degraded");
    }

    [Fact]
    public async Task GetHealth_WhenExceptionThrown_ReturnsInternalServerError()
    {
        // Arrange
        _mockHealthCheckService
            .Setup(x => x.CheckHealthAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Health check failed"));

        // Act
        var result = await _controller.GetHealth();

        // Assert
        result.Should().NotBeNull();
        result.Result.Should().BeOfType<ObjectResult>();
        
        var objResult = result.Result as ObjectResult;
        objResult!.StatusCode.Should().Be(503); 
    }

    [Fact]
    public async Task GetHealth_IncludesResponseTime()
    {
        // Arrange
        var healthReport = new HealthReport(
            new Dictionary<string, HealthReportEntry>(),
            HealthStatus.Healthy,
            TimeSpan.FromMilliseconds(100));

        _mockHealthCheckService
            .Setup(x => x.CheckHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(healthReport);

        // Act
        var result = await _controller.GetHealth();

        // Assert
        var objResult = result.Result as ObjectResult;
        objResult.Should().NotBeNull("response should not be null");
        
        var response = objResult!.Value as HealthCheckResponse;
        
        response!.Duration.Should().BeGreaterThan(TimeSpan.Zero);
        response.Status.Should().Be("Healthy");
    }

    [Fact]
    public async Task GetHealth_IncludesChecksInformation()
    {
        // Arrange
        var healthReport = new HealthReport(
            new Dictionary<string, HealthReportEntry>
            {
                ["database"] = new HealthReportEntry(
                    HealthStatus.Healthy,
                    "Database connection successful",
                    TimeSpan.FromMilliseconds(25),
                    null,
                    new Dictionary<string, object> { ["server"] = "localhost" }),
                ["storage"] = new HealthReportEntry(
                    HealthStatus.Healthy,
                    "Storage service available",
                    TimeSpan.FromMilliseconds(30),
                    null,
                    new Dictionary<string, object>())
            },
            HealthStatus.Healthy,
            TimeSpan.FromMilliseconds(100));

        _mockHealthCheckService
            .Setup(x => x.CheckHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(healthReport);

        // Act
        var result = await _controller.GetHealth();

        // Assert
        var objResult = result.Result as ObjectResult;
        objResult.Should().NotBeNull("objectResult should not be null");

        var response = objResult!.Value as HealthCheckResponse;
        response.Should().NotBeNull("response should not be null");

        response!.Results.Should().HaveCount(2);
        response.Results.Should().ContainKey("database");
        response.Results.Should().ContainKey("storage");
        
        response.Status.Should().Be("Healthy");
        response.Duration.Should().BeGreaterThan(TimeSpan.Zero);
    }
}