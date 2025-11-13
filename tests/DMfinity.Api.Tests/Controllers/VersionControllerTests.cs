using Mystira.App.Api.Controllers;
using Mystira.App.Api.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DMfinity.Api.Tests.Controllers;

public class VersionControllerTests
{
    private readonly Mock<IWebHostEnvironment> _mockEnvironment;
    private readonly Mock<ILogger<VersionController>> _mockLogger;
    private readonly VersionController _controller;

    public VersionControllerTests()
    {
        _mockEnvironment = new Mock<IWebHostEnvironment>();
        _mockEnvironment.Setup(x => x.EnvironmentName).Returns("Test");
        _mockLogger = new Mock<ILogger<VersionController>>();
        _controller = new VersionController(_mockEnvironment.Object, _mockLogger.Object);
    }

    [Fact]
    public void GetVersion_ReturnsOkResult()
    {
        // Act
        var result = _controller.GetVersion();

        // Assert
        result.Should().NotBeNull();
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public void GetVersion_ReturnsVersionInfo()
    {
        // Act
        var result = _controller.GetVersion();

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().BeOfType<VersionInfo>();

        var versionInfo = okResult.Value as VersionInfo;
        versionInfo.Should().NotBeNull();
        versionInfo!.Version.Should().NotBeNullOrEmpty();
        versionInfo.ApiName.Should().Be("Mystira.App.Api");
        versionInfo.BuildDate.Should().NotBeNullOrEmpty();
        versionInfo.Environment.Should().Be("Test");
    }

    [Fact]
    public void GetVersion_IncludesEnvironmentName()
    {
        // Arrange
        _mockEnvironment.Setup(x => x.EnvironmentName).Returns("Production");
        var controller = new VersionController(_mockEnvironment.Object, _mockLogger.Object);

        // Act
        var result = controller.GetVersion();

        // Assert
        var okResult = result.Result as OkObjectResult;
        var versionInfo = okResult!.Value as VersionInfo;
        versionInfo!.Environment.Should().Be("Production");
    }

    [Fact]
    public void GetVersion_ReturnsValidVersionFormat()
    {
        // Act
        var result = _controller.GetVersion();

        // Assert
        var okResult = result.Result as OkObjectResult;
        var versionInfo = okResult!.Value as VersionInfo;
        
        // Version should be in format like "1.0.0.0"
        versionInfo!.Version.Should().MatchRegex(@"^\d+\.\d+\.\d+\.\d+$");
    }

    [Fact]
    public void GetVersion_ReturnsValidBuildDateFormat()
    {
        // Act
        var result = _controller.GetVersion();

        // Assert
        var okResult = result.Result as OkObjectResult;
        var versionInfo = okResult!.Value as VersionInfo;
        
        // BuildDate should contain "UTC"
        versionInfo!.BuildDate.Should().Contain("UTC");
    }
}
