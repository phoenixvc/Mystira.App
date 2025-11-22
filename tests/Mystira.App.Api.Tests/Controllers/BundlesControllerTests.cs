using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Mystira.App.Api.Controllers;
using Mystira.App.Api.Services;
using Mystira.App.Domain.Models;
using Xunit;

namespace Mystira.App.Api.Tests.Controllers;

public class BundlesControllerTests
{
    private static BundlesController CreateController(Mock<IContentBundleService> serviceMock)
    {
        var logger = new Mock<ILogger<BundlesController>>().Object;
        var controller = new BundlesController(serviceMock.Object, logger)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        return controller;
    }

    [Fact]
    public async Task GetBundles_ReturnsOk_WithBundles()
    {
        // Arrange
        var bundles = new List<ContentBundle>
        {
            new() { Id = "b1", Title = "Bundle 1", AgeGroup = "6-9", ScenarioIds = new List<string>{"s1","s2"}, ImageId = "img1" },
            new() { Id = "b2", Title = "Bundle 2", AgeGroup = "10-12", ScenarioIds = new List<string>{"s3"}, ImageId = "img2", IsFree = true }
        };
        var service = new Mock<IContentBundleService>();
        service.Setup(s => s.GetAllAsync()).ReturnsAsync(bundles);
        var controller = CreateController(service);

        // Act
        var result = await controller.GetBundles();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var ok = result.Result as OkObjectResult;
        ok!.Value.Should().BeEquivalentTo(bundles);
        service.Verify(s => s.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetBundles_WhenServiceThrows_Returns500_WithTraceId()
    {
        // Arrange
        var service = new Mock<IContentBundleService>();
        service.Setup(s => s.GetAllAsync()).ThrowsAsync(new System.Exception("boom"));
        var controller = CreateController(service);

        // Act
        var result = await controller.GetBundles();

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var obj = result.Result as ObjectResult;
        obj!.StatusCode.Should().Be(500);
        obj.Value.Should().NotBeNull();
        obj.Value!.ToString().Should().Contain("TraceId");
        service.Verify(s => s.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetBundlesByAgeGroup_ReturnsOk_WithFilteredBundles()
    {
        // Arrange
        var ageGroup = "6-9";
        var bundles = new List<ContentBundle>
        {
            new() { Id = "b1", Title = "Bundle 1", AgeGroup = ageGroup, ScenarioIds = new List<string>{"s1"}, ImageId = "img1" }
        };
        var service = new Mock<IContentBundleService>();
        service.Setup(s => s.GetByAgeGroupAsync(ageGroup)).ReturnsAsync(bundles);
        var controller = CreateController(service);

        // Act
        var result = await controller.GetBundlesByAgeGroup(ageGroup);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var ok = result.Result as OkObjectResult;
        ok!.Value.Should().BeEquivalentTo(bundles);
        service.Verify(s => s.GetByAgeGroupAsync(ageGroup), Times.Once);
    }

    [Fact]
    public async Task GetBundlesByAgeGroup_WhenServiceThrows_Returns500_WithTraceId()
    {
        // Arrange
        var ageGroup = "3-5";
        var service = new Mock<IContentBundleService>();
        service.Setup(s => s.GetByAgeGroupAsync(ageGroup)).ThrowsAsync(new System.Exception("boom"));
        var controller = CreateController(service);

        // Act
        var result = await controller.GetBundlesByAgeGroup(ageGroup);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var obj = result.Result as ObjectResult;
        obj!.StatusCode.Should().Be(500);
        obj.Value.Should().NotBeNull();
        obj.Value!.ToString().Should().Contain("TraceId");
        service.Verify(s => s.GetByAgeGroupAsync(ageGroup), Times.Once);
    }
}
