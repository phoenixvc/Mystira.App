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
using Mystira.App.Application.CQRS.Attribution.Queries;
using Mystira.App.Application.CQRS.Scenarios.Queries;
using Mystira.App.Domain.Models;
using Mystira.Contracts.App.Requests.Scenarios;
using Mystira.Contracts.App.Responses.Attribution;
using Mystira.Contracts.App.Responses.Scenarios;
using Wolverine;
using Xunit;

namespace Mystira.App.Api.Tests.Controllers;

public class ScenariosControllerTests
{
    private readonly Mock<IMessageBus> _mockBus;
    private readonly Mock<ILogger<ScenariosController>> _mockLogger;
    private readonly ScenariosController _controller;

    public ScenariosControllerTests()
    {
        _mockBus = new Mock<IMessageBus>();
        _mockLogger = new Mock<ILogger<ScenariosController>>();
        _controller = new ScenariosController(_mockBus.Object, _mockLogger.Object);

        // Set up HttpContext for TraceIdentifier
        var httpContext = new DefaultHttpContext();
        httpContext.TraceIdentifier = "test-trace-id";
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    #region GetScenarios Tests

    [Fact]
    public async Task GetScenarios_ReturnsOkWithScenarioList()
    {
        // Arrange
        var request = new ScenarioQueryRequest { Page = 1, PageSize = 10 };
        var expectedResponse = new ScenarioListResponse
        {
            Scenarios = new List<Scenario>
            {
                new Scenario { Id = "scenario-1", Title = "Test Scenario 1" },
                new Scenario { Id = "scenario-2", Title = "Test Scenario 2" }
            },
            TotalCount = 2,
            Page = 1,
            PageSize = 10
        };

        _mockBus
            .Setup(x => x.InvokeAsync<ScenarioListResponse>(
                It.IsAny<GetPaginatedScenariosQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetScenarios(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ScenarioListResponse>().Subject;
        response.Scenarios.Should().HaveCount(2);
        response.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetScenarios_WithFilters_PassesFiltersToQuery()
    {
        // Arrange
        var request = new ScenarioQueryRequest
        {
            Page = 2,
            PageSize = 20,
            Search = "dragon",
            AgeGroup = "8-12",
            Genre = "fantasy"
        };

        GetPaginatedScenariosQuery? capturedQuery = null;
        _mockBus
            .Setup(x => x.InvokeAsync<ScenarioListResponse>(
                It.IsAny<GetPaginatedScenariosQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .Callback<GetPaginatedScenariosQuery, CancellationToken, TimeSpan?>((q, _, _) => capturedQuery = q)
            .ReturnsAsync(new ScenarioListResponse());

        // Act
        await _controller.GetScenarios(request);

        // Assert
        capturedQuery.Should().NotBeNull();
        capturedQuery!.Page.Should().Be(2);
        capturedQuery.PageSize.Should().Be(20);
        capturedQuery.Search.Should().Be("dragon");
        capturedQuery.AgeGroup.Should().Be("8-12");
        capturedQuery.Genre.Should().Be("fantasy");
    }

    [Fact]
    public async Task GetScenarios_WhenExceptionThrown_ReturnsInternalServerError()
    {
        // Arrange
        var request = new ScenarioQueryRequest();
        _mockBus
            .Setup(x => x.InvokeAsync<ScenarioListResponse>(
                It.IsAny<GetPaginatedScenariosQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetScenarios(request);

        // Assert
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region GetScenario Tests

    [Fact]
    public async Task GetScenario_WhenScenarioExists_ReturnsOkWithScenario()
    {
        // Arrange
        var scenarioId = "scenario-1";
        var scenario = new Scenario { Id = scenarioId, Title = "Test Scenario" };

        _mockBus
            .Setup(x => x.InvokeAsync<Scenario?>(
                It.IsAny<GetScenarioQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(scenario);

        // Act
        var result = await _controller.GetScenario(scenarioId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedScenario = okResult.Value.Should().BeOfType<Scenario>().Subject;
        returnedScenario.Id.Should().Be(scenarioId);
    }

    [Fact]
    public async Task GetScenario_WhenScenarioNotFound_ReturnsNotFound()
    {
        // Arrange
        var scenarioId = "nonexistent";

        _mockBus
            .Setup(x => x.InvokeAsync<Scenario?>(
                It.IsAny<GetScenarioQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(default(Scenario));

        // Act
        var result = await _controller.GetScenario(scenarioId);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetScenario_WhenExceptionThrown_ReturnsInternalServerError()
    {
        // Arrange
        var scenarioId = "scenario-1";

        _mockBus
            .Setup(x => x.InvokeAsync<Scenario?>(
                It.IsAny<GetScenarioQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetScenario(scenarioId);

        // Assert
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region GetScenariosByAgeGroup Tests

    [Fact]
    public async Task GetScenariosByAgeGroup_ReturnsOkWithScenarios()
    {
        // Arrange
        var ageGroup = "8-12";
        var scenarios = new List<Scenario>
        {
            new Scenario { Id = "scenario-1", Title = "Kids Adventure" },
            new Scenario { Id = "scenario-2", Title = "Dragon Quest" }
        };

        _mockBus
            .Setup(x => x.InvokeAsync<List<Scenario>>(
                It.IsAny<GetScenariosByAgeGroupQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(scenarios);

        // Act
        var result = await _controller.GetScenariosByAgeGroup(ageGroup);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedScenarios = okResult.Value.Should().BeOfType<List<Scenario>>().Subject;
        returnedScenarios.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetScenariosByAgeGroup_WhenExceptionThrown_ReturnsInternalServerError()
    {
        // Arrange
        var ageGroup = "8-12";

        _mockBus
            .Setup(x => x.InvokeAsync<List<Scenario>>(
                It.IsAny<GetScenariosByAgeGroupQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetScenariosByAgeGroup(ageGroup);

        // Assert
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region GetFeaturedScenarios Tests

    [Fact]
    public async Task GetFeaturedScenarios_ReturnsOkWithScenarios()
    {
        // Arrange
        var scenarios = new List<Scenario>
        {
            new Scenario { Id = "featured-1", Title = "Featured Adventure" }
        };

        _mockBus
            .Setup(x => x.InvokeAsync<List<Scenario>>(
                It.IsAny<GetFeaturedScenariosQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(scenarios);

        // Act
        var result = await _controller.GetFeaturedScenarios();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedScenarios = okResult.Value.Should().BeOfType<List<Scenario>>().Subject;
        returnedScenarios.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetFeaturedScenarios_WhenExceptionThrown_ReturnsInternalServerError()
    {
        // Arrange
        _mockBus
            .Setup(x => x.InvokeAsync<List<Scenario>>(
                It.IsAny<GetFeaturedScenariosQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetFeaturedScenarios();

        // Assert
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region GetScenariosWithGameState Tests

    [Fact]
    public async Task GetScenariosWithGameState_ReturnsOkWithResponse()
    {
        // Arrange
        var accountId = "account-1";
        var expectedResponse = new ScenarioGameStateResponse
        {
            Scenarios = new List<ScenarioWithGameState>()
        };

        _mockBus
            .Setup(x => x.InvokeAsync<ScenarioGameStateResponse>(
                It.IsAny<GetScenariosWithGameStateQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetScenariosWithGameState(accountId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeOfType<ScenarioGameStateResponse>();
    }

    [Fact]
    public async Task GetScenariosWithGameState_WhenExceptionThrown_ReturnsInternalServerError()
    {
        // Arrange
        var accountId = "account-1";

        _mockBus
            .Setup(x => x.InvokeAsync<ScenarioGameStateResponse>(
                It.IsAny<GetScenariosWithGameStateQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetScenariosWithGameState(accountId);

        // Assert
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region GetScenarioAttribution Tests

    [Fact]
    public async Task GetScenarioAttribution_WhenScenarioExists_ReturnsOkWithAttribution()
    {
        // Arrange
        var scenarioId = "scenario-1";
        var attribution = new ContentAttributionResponse
        {
            ScenarioId = scenarioId,
            Title = "Test Scenario"
        };

        _mockBus
            .Setup(x => x.InvokeAsync<ContentAttributionResponse?>(
                It.IsAny<GetScenarioAttributionQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(attribution);

        // Act
        var result = await _controller.GetScenarioAttribution(scenarioId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedAttribution = okResult.Value.Should().BeOfType<ContentAttributionResponse>().Subject;
        returnedAttribution.ScenarioId.Should().Be(scenarioId);
    }

    [Fact]
    public async Task GetScenarioAttribution_WhenScenarioNotFound_ReturnsNotFound()
    {
        // Arrange
        var scenarioId = "nonexistent";

        _mockBus
            .Setup(x => x.InvokeAsync<ContentAttributionResponse?>(
                It.IsAny<GetScenarioAttributionQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(default(ContentAttributionResponse));

        // Act
        var result = await _controller.GetScenarioAttribution(scenarioId);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region GetScenarioIpStatus Tests

    [Fact]
    public async Task GetScenarioIpStatus_WhenScenarioExists_ReturnsOkWithIpStatus()
    {
        // Arrange
        var scenarioId = "scenario-1";
        var ipStatus = new IpVerificationResponse
        {
            ScenarioId = scenarioId,
            IsRegistered = true
        };

        _mockBus
            .Setup(x => x.InvokeAsync<IpVerificationResponse?>(
                It.IsAny<GetScenarioIpStatusQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(ipStatus);

        // Act
        var result = await _controller.GetScenarioIpStatus(scenarioId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedIpStatus = okResult.Value.Should().BeOfType<IpVerificationResponse>().Subject;
        returnedIpStatus.ScenarioId.Should().Be(scenarioId);
        returnedIpStatus.IsRegistered.Should().BeTrue();
    }

    [Fact]
    public async Task GetScenarioIpStatus_WhenScenarioNotFound_ReturnsNotFound()
    {
        // Arrange
        var scenarioId = "nonexistent";

        _mockBus
            .Setup(x => x.InvokeAsync<IpVerificationResponse?>(
                It.IsAny<GetScenarioIpStatusQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(default(IpVerificationResponse));

        // Act
        var result = await _controller.GetScenarioIpStatus(scenarioId);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion
}
