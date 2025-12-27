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
using Mystira.App.Application.CQRS.CompassAxes.Queries;
using Mystira.App.Domain.Models;
using Wolverine;
using Xunit;

namespace Mystira.App.Api.Tests.Controllers;

public class CompassAxesControllerTests
{
    private static CompassAxesController CreateController(Mock<IMessageBus> busMock)
    {
        var logger = new Mock<ILogger<CompassAxesController>>().Object;
        var controller = new CompassAxesController(busMock.Object, logger)
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
        var bus = new Mock<IMessageBus>();
        bus.Setup(m => m.InvokeAsync<List<CompassAxis>>(It.IsAny<GetAllCompassAxesQuery>(), It.IsAny<CancellationToken>(), It.IsAny<TimeSpan?>()))
            .ReturnsAsync(axes);
        var controller = CreateController(bus);

        // Act
        var result = await controller.GetAllCompassAxes();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var ok = result.Result as OkObjectResult;
        ok!.Value.Should().BeEquivalentTo(axes);
        bus.Verify(m => m.InvokeAsync<List<CompassAxis>>(It.IsAny<GetAllCompassAxesQuery>(), It.IsAny<CancellationToken>(), It.IsAny<TimeSpan?>()), Times.Once);
    }

    [Fact]
    public async Task GetAllCompassAxes_WhenEmpty_ReturnsEmptyList()
    {
        // Arrange
        var bus = new Mock<IMessageBus>();
        bus.Setup(m => m.InvokeAsync<List<CompassAxis>>(It.IsAny<GetAllCompassAxesQuery>(), It.IsAny<CancellationToken>(), It.IsAny<TimeSpan?>()))
            .ReturnsAsync(new List<CompassAxis>());
        var controller = CreateController(bus);

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
        var bus = new Mock<IMessageBus>();
        bus.Setup(m => m.InvokeAsync<CompassAxis?>(It.IsAny<GetCompassAxisByIdQuery>(), It.IsAny<CancellationToken>(), It.IsAny<TimeSpan?>()))
            .ReturnsAsync(axis);
        var controller = CreateController(bus);

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
        var bus = new Mock<IMessageBus>();
        bus.Setup(m => m.InvokeAsync<CompassAxis?>(It.IsAny<GetCompassAxisByIdQuery>(), It.IsAny<CancellationToken>(), It.IsAny<TimeSpan?>()))
            .ReturnsAsync((CompassAxis?)null);
        var controller = CreateController(bus);

        // Act
        var result = await controller.GetCompassAxisById("non-existent");

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task ValidateCompassAxis_ReturnsTrue_WhenValid()
    {
        // Arrange
        var bus = new Mock<IMessageBus>();
        bus.Setup(m => m.InvokeAsync<bool>(It.IsAny<ValidateCompassAxisQuery>(), It.IsAny<CancellationToken>(), It.IsAny<TimeSpan?>()))
            .ReturnsAsync(true);
        var controller = CreateController(bus);

        // Act
        var result = await controller.ValidateCompassAxis(new ValidateCompassAxisRequest { Name = "Courage" });

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ValidateCompassAxis_ReturnsFalse_WhenInvalid()
    {
        // Arrange
        var bus = new Mock<IMessageBus>();
        bus.Setup(m => m.InvokeAsync<bool>(It.IsAny<ValidateCompassAxisQuery>(), It.IsAny<CancellationToken>(), It.IsAny<TimeSpan?>()))
            .ReturnsAsync(false);
        var controller = CreateController(bus);

        // Act
        var result = await controller.ValidateCompassAxis(new ValidateCompassAxisRequest { Name = "NonExistent" });

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }
}
