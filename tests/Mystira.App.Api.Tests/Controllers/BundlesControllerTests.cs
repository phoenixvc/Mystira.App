using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Mystira.App.Api.Controllers;
using Mystira.App.Application.CQRS.ContentBundles.Queries;
using Mystira.App.Domain.Models;
using Xunit;

namespace Mystira.App.Api.Tests.Controllers;

public class BundlesControllerTests
{
    private static BundlesController CreateController(Mock<IMediator> mediatorMock)
    {
        var logger = new Mock<ILogger<BundlesController>>().Object;
        var controller = new BundlesController(mediatorMock.Object, logger)
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
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetAllContentBundlesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(bundles);
        var controller = CreateController(mediator);

        // Act
        var result = await controller.GetBundles();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var ok = result.Result as OkObjectResult;
        ok!.Value.Should().BeEquivalentTo(bundles);
        mediator.Verify(m => m.Send(It.IsAny<GetAllContentBundlesQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetBundles_WhenMediatorThrows_Returns500_WithTraceId()
    {
        // Arrange
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetAllContentBundlesQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new System.Exception("boom"));
        var controller = CreateController(mediator);

        // Act
        var result = await controller.GetBundles();

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var obj = result.Result as ObjectResult;
        obj!.StatusCode.Should().Be(500);
        obj.Value.Should().NotBeNull();
        obj.Value!.ToString().Should().Contain("TraceId");
        mediator.Verify(m => m.Send(It.IsAny<GetAllContentBundlesQuery>(), It.IsAny<CancellationToken>()), Times.Once);
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
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.Is<GetContentBundlesByAgeGroupQuery>(q => q.AgeGroup == ageGroup), It.IsAny<CancellationToken>()))
            .ReturnsAsync(bundles);
        var controller = CreateController(mediator);

        // Act
        var result = await controller.GetBundlesByAgeGroup(ageGroup);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var ok = result.Result as OkObjectResult;
        ok!.Value.Should().BeEquivalentTo(bundles);
        mediator.Verify(m => m.Send(It.Is<GetContentBundlesByAgeGroupQuery>(q => q.AgeGroup == ageGroup), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetBundlesByAgeGroup_WhenMediatorThrows_Returns500_WithTraceId()
    {
        // Arrange
        var ageGroup = "3-5";
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.Is<GetContentBundlesByAgeGroupQuery>(q => q.AgeGroup == ageGroup), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new System.Exception("boom"));
        var controller = CreateController(mediator);

        // Act
        var result = await controller.GetBundlesByAgeGroup(ageGroup);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var obj = result.Result as ObjectResult;
        obj!.StatusCode.Should().Be(500);
        obj.Value.Should().NotBeNull();
        obj.Value!.ToString().Should().Contain("TraceId");
        mediator.Verify(m => m.Send(It.Is<GetContentBundlesByAgeGroupQuery>(q => q.AgeGroup == ageGroup), It.IsAny<CancellationToken>()), Times.Once);
    }
}
