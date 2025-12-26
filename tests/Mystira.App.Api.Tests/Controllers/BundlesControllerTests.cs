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
using Mystira.App.Application.CQRS.ContentBundles.Queries;
using Mystira.App.Domain.Models;
using Wolverine;
using Xunit;

namespace Mystira.App.Api.Tests.Controllers;

public class BundlesControllerTests
{
    private static BundlesController CreateController(Mock<IMessageBus> busMock)
    {
        var logger = new Mock<ILogger<BundlesController>>().Object;
        var controller = new BundlesController(busMock.Object, logger)
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
        var bus = new Mock<IMessageBus>();
        bus.Setup(m => m.InvokeAsync<List<ContentBundle>>(It.IsAny<GetAllContentBundlesQuery>(), It.IsAny<CancellationToken>(), It.IsAny<TimeSpan?>()))
            .ReturnsAsync(bundles);
        var controller = CreateController(bus);

        // Act
        var result = await controller.GetBundles();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var ok = result.Result as OkObjectResult;
        ok!.Value.Should().BeEquivalentTo(bundles);
        bus.Verify(m => m.InvokeAsync<List<ContentBundle>>(It.IsAny<GetAllContentBundlesQuery>(), It.IsAny<CancellationToken>(), It.IsAny<TimeSpan?>()), Times.Once);
    }

    [Fact]
    public async Task GetBundles_WhenMessageBusThrows_Returns500_WithTraceId()
    {
        // Arrange
        var bus = new Mock<IMessageBus>();
        bus.Setup(m => m.InvokeAsync<List<ContentBundle>>(It.IsAny<GetAllContentBundlesQuery>(), It.IsAny<CancellationToken>(), It.IsAny<TimeSpan?>()))
            .ThrowsAsync(new Exception("boom"));
        var controller = CreateController(bus);

        // Act
        var result = await controller.GetBundles();

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var obj = result.Result as ObjectResult;
        obj!.StatusCode.Should().Be(500);
        obj.Value.Should().NotBeNull();
        obj.Value!.ToString().Should().Contain("TraceId");
        bus.Verify(m => m.InvokeAsync<List<ContentBundle>>(It.IsAny<GetAllContentBundlesQuery>(), It.IsAny<CancellationToken>(), It.IsAny<TimeSpan?>()), Times.Once);
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
        var bus = new Mock<IMessageBus>();
        bus.Setup(m => m.InvokeAsync<List<ContentBundle>>(It.Is<GetContentBundlesByAgeGroupQuery>(q => q.AgeGroup == ageGroup), It.IsAny<CancellationToken>(), It.IsAny<TimeSpan?>()))
            .ReturnsAsync(bundles);
        var controller = CreateController(bus);

        // Act
        var result = await controller.GetBundlesByAgeGroup(ageGroup);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var ok = result.Result as OkObjectResult;
        ok!.Value.Should().BeEquivalentTo(bundles);
        bus.Verify(m => m.InvokeAsync<List<ContentBundle>>(It.Is<GetContentBundlesByAgeGroupQuery>(q => q.AgeGroup == ageGroup), It.IsAny<CancellationToken>(), It.IsAny<TimeSpan?>()), Times.Once);
    }

    [Fact]
    public async Task GetBundlesByAgeGroup_WhenMessageBusThrows_Returns500_WithTraceId()
    {
        // Arrange
        var ageGroup = "3-5";
        var bus = new Mock<IMessageBus>();
        bus.Setup(m => m.InvokeAsync<List<ContentBundle>>(It.Is<GetContentBundlesByAgeGroupQuery>(q => q.AgeGroup == ageGroup), It.IsAny<CancellationToken>(), It.IsAny<TimeSpan?>()))
            .ThrowsAsync(new Exception("boom"));
        var controller = CreateController(bus);

        // Act
        var result = await controller.GetBundlesByAgeGroup(ageGroup);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var obj = result.Result as ObjectResult;
        obj!.StatusCode.Should().Be(500);
        obj.Value.Should().NotBeNull();
        obj.Value!.ToString().Should().Contain("TraceId");
        bus.Verify(m => m.InvokeAsync<List<ContentBundle>>(It.Is<GetContentBundlesByAgeGroupQuery>(q => q.AgeGroup == ageGroup), It.IsAny<CancellationToken>(), It.IsAny<TimeSpan?>()), Times.Once);
    }
}
