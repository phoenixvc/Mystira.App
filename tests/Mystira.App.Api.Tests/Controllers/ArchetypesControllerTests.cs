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
using Mystira.App.Application.CQRS.Archetypes.Queries;
using Mystira.App.Domain.Models;
using Xunit;

namespace Mystira.App.Api.Tests.Controllers;

public class ArchetypesControllerTests
{
    private static ArchetypesController CreateController(Mock<IMediator> mediatorMock)
    {
        var logger = new Mock<ILogger<ArchetypesController>>().Object;
        var controller = new ArchetypesController(mediatorMock.Object, logger)
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
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetAllArchetypesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(archetypes);
        var controller = CreateController(mediator);

        // Act
        var result = await controller.GetAllArchetypes();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var ok = result.Result as OkObjectResult;
        ok!.Value.Should().BeEquivalentTo(archetypes);
        mediator.Verify(m => m.Send(It.IsAny<GetAllArchetypesQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllArchetypes_WhenEmpty_ReturnsEmptyList()
    {
        // Arrange
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetAllArchetypesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ArchetypeDefinition>());
        var controller = CreateController(mediator);

        // Act
        var result = await controller.GetAllArchetypes();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var ok = result.Result as OkObjectResult;
        var value = ok!.Value as List<ArchetypeDefinition>;
        value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetArchetypeById_ReturnsOk_WhenFound()
    {
        // Arrange
        var archetype = new ArchetypeDefinition { Id = "arch-1", Name = "Hero", Description = "The protagonist" };
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetArchetypeByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(archetype);
        var controller = CreateController(mediator);

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
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetArchetypeByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ArchetypeDefinition?)null);
        var controller = CreateController(mediator);

        // Act
        var result = await controller.GetArchetypeById("non-existent");

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task ValidateArchetype_ReturnsTrue_WhenValid()
    {
        // Arrange
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<ValidateArchetypeQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var controller = CreateController(mediator);

        // Act
        var result = await controller.ValidateArchetype(new ValidateArchetypeRequest { Name = "Hero" });

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }
}
