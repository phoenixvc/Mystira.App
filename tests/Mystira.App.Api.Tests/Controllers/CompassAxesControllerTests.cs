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
using Mystira.App.Application.CQRS.CompassAxes.Queries;
using Mystira.App.Domain.Models;
using Xunit;

namespace Mystira.App.Api.Tests.Controllers;

public class CompassAxesControllerTests
{
    private static CompassAxesController CreateController(Mock<IMediator> mediatorMock)
    {
        var logger = new Mock<ILogger<CompassAxesController>>().Object;
        var controller = new CompassAxesController(mediatorMock.Object, logger)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        return controller;
    }

    [Fact]
    public async Task GetAllCompassAxes_ReturnsOk_WithAxes()
    {
        // Arrange
        var axes = new List<CompassAxis>
        {
            new() { Id = "axis-1", Name = "Courage", Description = "Measures bravery" },
            new() { Id = "axis-2", Name = "Wisdom", Description = "Measures knowledge" }
        };
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetAllCompassAxesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(axes);
        var controller = CreateController(mediator);

        // Act
        var result = await controller.GetAllCompassAxes();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var ok = result.Result as OkObjectResult;
        ok!.Value.Should().BeEquivalentTo(axes);
        mediator.Verify(m => m.Send(It.IsAny<GetAllCompassAxesQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllCompassAxes_WhenEmpty_ReturnsEmptyList()
    {
        // Arrange
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetAllCompassAxesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CompassAxis>());
        var controller = CreateController(mediator);

        // Act
        var result = await controller.GetAllCompassAxes();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var ok = result.Result as OkObjectResult;
        var value = ok!.Value as List<CompassAxis>;
        value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCompassAxisById_ReturnsOk_WhenFound()
    {
        // Arrange
        var axis = new CompassAxis { Id = "axis-1", Name = "Courage", Description = "Measures bravery" };
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetCompassAxisByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(axis);
        var controller = CreateController(mediator);

        // Act
        var result = await controller.GetCompassAxisById("axis-1");

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var ok = result.Result as OkObjectResult;
        ok!.Value.Should().BeEquivalentTo(axis);
    }

    [Fact]
    public async Task GetCompassAxisById_ReturnsNotFound_WhenNotExists()
    {
        // Arrange
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetCompassAxisByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CompassAxis?)null);
        var controller = CreateController(mediator);

        // Act
        var result = await controller.GetCompassAxisById("non-existent");

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task ValidateCompassAxis_ReturnsTrue_WhenValid()
    {
        // Arrange
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<ValidateCompassAxisQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var controller = CreateController(mediator);

        // Act
        var result = await controller.ValidateCompassAxis(new ValidateCompassAxisRequest { Name = "Courage" });

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ValidateCompassAxis_ReturnsFalse_WhenInvalid()
    {
        // Arrange
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<ValidateCompassAxisQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var controller = CreateController(mediator);

        // Act
        var result = await controller.ValidateCompassAxis(new ValidateCompassAxisRequest { Name = "NonExistent" });

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }
}
