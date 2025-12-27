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
using Mystira.App.Application.CQRS.Archetypes.Queries;
using Mystira.App.Domain.Models;
using Wolverine;
using Xunit;

namespace Mystira.App.Api.Tests.Controllers;

public class ArchetypesControllerTests
{
    private static ArchetypesController CreateController(Mock<IMessageBus> busMock)
    {
        var logger = new Mock<ILogger<ArchetypesController>>().Object;
        var controller = new ArchetypesController(busMock.Object, logger)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        return controller;
    }

    [Fact]
    public async Task GetAllArchetypes_ReturnsOk_WithArchetypes()
    {
        // Arrange
        var archetypes = new List<ArchetypeDefinition>
        {
            new() { Id = "arch-1", Name = "Hero", Description = "The protagonist" },
            new() { Id = "arch-2", Name = "Mentor", Description = "The wise guide" }
        };
        var bus = new Mock<IMessageBus>();
        bus.Setup(m => m.InvokeAsync<List<ArchetypeDefinition>>(It.IsAny<GetAllArchetypesQuery>(), It.IsAny<CancellationToken>(), It.IsAny<TimeSpan?>()))
            .ReturnsAsync(archetypes);
        var controller = CreateController(bus);

        // Act
        var result = await controller.GetAllArchetypes();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var ok = result.Result as OkObjectResult;
        ok!.Value.Should().BeEquivalentTo(archetypes);
        bus.Verify(m => m.InvokeAsync<List<ArchetypeDefinition>>(It.IsAny<GetAllArchetypesQuery>(), It.IsAny<CancellationToken>(), It.IsAny<TimeSpan?>()), Times.Once);
    }

    [Fact]
    public async Task GetAllArchetypes_WhenEmpty_ReturnsEmptyList()
    {
        // Arrange
        var bus = new Mock<IMessageBus>();
        bus.Setup(m => m.InvokeAsync<List<ArchetypeDefinition>>(It.IsAny<GetAllArchetypesQuery>(), It.IsAny<CancellationToken>(), It.IsAny<TimeSpan?>()))
            .ReturnsAsync(new List<ArchetypeDefinition>());
        var controller = CreateController(bus);

        // Act
        var result = await controller.GetAllArchetypes();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var ok = result.Result as OkObjectResult;
        var value = ok!.Value as List<ArchetypeDefinition>;
        value.Should().NotBeNull();
        value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetArchetypeById_ReturnsOk_WhenFound()
    {
        // Arrange
        var archetype = new ArchetypeDefinition { Id = "arch-1", Name = "Hero", Description = "The protagonist" };
        var bus = new Mock<IMessageBus>();
        bus.Setup(m => m.InvokeAsync<ArchetypeDefinition?>(It.IsAny<GetArchetypeByIdQuery>(), It.IsAny<CancellationToken>(), It.IsAny<TimeSpan?>()))
            .ReturnsAsync(archetype);
        var controller = CreateController(bus);

        // Act
        var result = await controller.GetArchetypeById("arch-1");

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var ok = result.Result as OkObjectResult;
        ok!.Value.Should().BeEquivalentTo(archetype);
    }

    [Fact]
    public async Task GetArchetypeById_ReturnsNotFound_WhenNotExists()
    {
        // Arrange
        var bus = new Mock<IMessageBus>();
        bus.Setup(m => m.InvokeAsync<ArchetypeDefinition?>(It.IsAny<GetArchetypeByIdQuery>(), It.IsAny<CancellationToken>(), It.IsAny<TimeSpan?>()))
            .ReturnsAsync((ArchetypeDefinition?)null);
        var controller = CreateController(bus);

        // Act
        var result = await controller.GetArchetypeById("non-existent");

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task ValidateArchetype_ReturnsTrue_WhenValid()
    {
        // Arrange
        var bus = new Mock<IMessageBus>();
        bus.Setup(m => m.InvokeAsync<bool>(It.IsAny<ValidateArchetypeQuery>(), It.IsAny<CancellationToken>(), It.IsAny<TimeSpan?>()))
            .ReturnsAsync(true);
        var controller = CreateController(bus);

        // Act
        var result = await controller.ValidateArchetype(new ValidateArchetypeRequest { Name = "Hero" });

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }
}
