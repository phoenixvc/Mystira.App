// Example API Controller Test
// File: tests/Mystira.App.Api.Tests/Controllers/ScenariosControllerTests.cs

using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Mystira.App.Api.Controllers;
using Mystira.App.Application.CQRS.Scenarios.Commands;
using Mystira.App.Application.CQRS.Scenarios.Queries;
using Mystira.App.Contracts.Requests.Scenarios;
using Mystira.App.Contracts.Responses.Common;
using Mystira.App.Contracts.Responses.Scenarios;
using Xunit;

namespace Mystira.App.Api.Tests.Controllers;

/// <summary>
/// Example tests for an API controller
/// Demonstrates testing HTTP actions, status codes, and error handling
/// Uses Moq to mock MediatR and other dependencies
/// </summary>
public class ScenariosControllerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ILogger<ScenariosController>> _loggerMock;
    private readonly ScenariosController _controller;

    public ScenariosControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _loggerMock = new Mock<ILogger<ScenariosController>>();
        _controller = new ScenariosController(_mediatorMock.Object, _loggerMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    [Fact]
    public async Task GetScenarioById_WhenScenarioExists_ReturnsOk()
    {
        // Arrange
        var scenarioId = "scenario-1";
        var scenario = new ScenarioResponse
        {
            Id = scenarioId,
            Title = "The Dragon's Lair",
            Description = "Face the dragon",
            Genre = "Fantasy"
        };

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetScenarioQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(scenario);

        // Act
        var result = await _controller.GetScenarioById(scenarioId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(scenario);

        _mediatorMock.Verify(
            m => m.Send(It.Is<GetScenarioQuery>(q => q.ScenarioId == scenarioId), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetScenarioById_WhenScenarioNotFound_ReturnsNotFound()
    {
        // Arrange
        var scenarioId = "non-existent-id";
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetScenarioQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ScenarioResponse?)null);

        // Act
        var result = await _controller.GetScenarioById(scenarioId);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
        var notFound = (NotFoundObjectResult)result.Result;
        notFound.Value.Should().BeOfType<ErrorResponse>();
        var error = notFound.Value as ErrorResponse;
        error!.Message.Should().Contain(scenarioId);
    }

    [Fact]
    public async Task GetAllScenarios_ReturnsOkWithScenarios()
    {
        // Arrange
        var scenarios = new List<ScenarioResponse>
        {
            new() { Id = "scenario-1", Title = "Scenario 1" },
            new() { Id = "scenario-2", Title = "Scenario 2" }
        };

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetAllScenariosQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(scenarios);

        // Act
        var result = await _controller.GetAllScenarios();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().NotBeNull();
        okResult.Value.Should().BeOfType<List<ScenarioResponse>>();
        var returnedScenarios = (List<ScenarioResponse>)okResult.Value;
        returnedScenarios.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateScenario_WithValidRequest_ReturnsCreated()
    {
        // Arrange
        var request = new CreateScenarioRequest
        {
            Title = "New Scenario",
            Description = "A brand new scenario",
            Genre = "Fantasy",
            AgeGroup = "8-10"
        };

        var createdScenario = new ScenarioResponse
        {
            Id = "new-scenario-id",
            Title = request.Title,
            Description = request.Description,
            Genre = request.Genre
        };

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<CreateScenarioCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdScenario);

        // Act
        var result = await _controller.CreateScenario(request);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
        var created = result.Result as CreatedAtActionResult;
        created.Should().NotBeNull();
        created!.Value.Should().BeEquivalentTo(createdScenario);
        created.ActionName.Should().Be(nameof(_controller.GetScenarioById));
        created.RouteValues!["id"].Should().Be(createdScenario.Id);
    }

    [Fact]
    public async Task CreateScenario_WithInvalidRequest_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateScenarioRequest
        {
            Title = "", // Invalid: empty title
            Description = "Test",
            Genre = "Fantasy",
            AgeGroup = "8-10"
        };

        // Simulate model validation error
        _controller.ModelState.AddModelError("Title", "Title is required");

        // Act
        var result = await _controller.CreateScenario(request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task UpdateScenario_WhenScenarioExists_ReturnsOk()
    {
        // Arrange
        var scenarioId = "scenario-1";
        var request = new UpdateScenarioRequest
        {
            Title = "Updated Title",
            Description = "Updated description"
        };

        var updatedScenario = new ScenarioResponse
        {
            Id = scenarioId,
            Title = request.Title,
            Description = request.Description
        };

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<UpdateScenarioCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedScenario);

        // Act
        var result = await _controller.UpdateScenario(scenarioId, request);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(updatedScenario);
    }

    [Fact]
    public async Task DeleteScenario_WhenScenarioExists_ReturnsNoContent()
    {
        // Arrange
        var scenarioId = "scenario-1";
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<DeleteScenarioCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteScenario(scenarioId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteScenario_WhenScenarioNotFound_ReturnsNotFound()
    {
        // Arrange
        var scenarioId = "non-existent-id";
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<DeleteScenarioCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteScenario(scenarioId);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public void GetScenarioById_HasAllowAnonymousAttribute()
    {
        // Arrange
        var method = typeof(ScenariosController).GetMethod(nameof(ScenariosController.GetScenarioById));

        // Act
        var attributes = method!.GetCustomAttributes(
            typeof(Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute), false);

        // Assert
        attributes.Should().NotBeEmpty(
            "GetScenarioById should allow anonymous access for public scenarios");
    }

    [Fact]
    public async Task GetScenarioById_OnException_ReturnsInternalServerError()
    {
        // Arrange
        var scenarioId = "scenario-1";
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetScenarioQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database connection failed"));

        // Act
        var result = await _controller.GetScenarioById(scenarioId);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = result.Result.As<ObjectResult>();
        objectResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        objectResult.Value.Should().BeOfType<ErrorResponse>();

        // Verify error was logged
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }
}
