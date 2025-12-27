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
using Mystira.Contracts.App.Responses.Common;
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

        // Setup HttpContext for TraceIdentifier
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
        var response = new ScenarioListResponse
        {
            Items = new List<ScenarioSummary>
            {
                new ScenarioSummary { Id = "scenario-1", Title = "Adventure 1" },
                new ScenarioSummary { Id = "scenario-2", Title = "Adventure 2" }
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
            .ReturnsAsync(response);

        // Act
        var result = await _controller.GetScenarios(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedResponse = okResult.Value.Should().BeOfType<ScenarioListResponse>().Subject;
        returnedResponse.Items.Should().HaveCount(2);
        returnedResponse.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetScenarios_WithFilters_PassesFiltersToQuery()
    {
        // Arrange
        var request = new ScenarioQueryRequest
        {
            Page = 1,
            PageSize = 10,
            Search = "dragon",
            AgeGroup = "kids",
            Genre = "fantasy"
        };
        var response = new ScenarioListResponse { Items = new List<ScenarioSummary>(), TotalCount = 0 };

        _mockBus
            .Setup(x => x.InvokeAsync<ScenarioListResponse>(
                It.Is<GetPaginatedScenariosQuery>(q =>
                    q.Search == "dragon" && q.AgeGroup == "kids" && q.Genre == "fantasy"),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.GetScenarios(request);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        _mockBus.Verify(x => x.InvokeAsync<ScenarioListResponse>(
            It.Is<GetPaginatedScenariosQuery>(q =>
                q.Search == "dragon" && q.AgeGroup == "kids" && q.Genre == "fantasy"),
            It.IsAny<CancellationToken>(),
            It.IsAny<TimeSpan?>()), Times.Once);
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
        var scenario = new Scenario { Id = scenarioId, Title = "Test Adventure" };

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
            .ReturnsAsync((Scenario?)null);

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
        var ageGroup = "kids";
        var scenarios = new List<Scenario>
        {
            new Scenario { Id = "scenario-1", Title = "Kid Adventure 1" },
            new Scenario { Id = "scenario-2", Title = "Kid Adventure 2" }
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
        var ageGroup = "kids";

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
            new Scenario { Id = "featured-1", Title = "Featured Adventure", IsFeatured = true }
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
        returnedScenarios[0].IsFeatured.Should().BeTrue();
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
        var accountId = "acc-1";
        var response = new ScenarioGameStateResponse
        {
            Scenarios = new List<ScenarioWithGameState>
            {
                new ScenarioWithGameState { ScenarioId = "scenario-1", IsCompleted = true }
            }
        };

        _mockBus
            .Setup(x => x.InvokeAsync<ScenarioGameStateResponse>(
                It.IsAny<GetScenariosWithGameStateQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.GetScenariosWithGameState(accountId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedResponse = okResult.Value.Should().BeOfType<ScenarioGameStateResponse>().Subject;
        returnedResponse.Scenarios.Should().HaveCount(1);
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
            ContentId = scenarioId,
            ContentType = "Scenario",
            Contributors = new List<ContributorCredit>
            {
                new ContributorCredit { Name = "Author Name", Role = "Writer" }
            }
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
        returnedAttribution.ContentId.Should().Be(scenarioId);
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
            .ReturnsAsync((ContentAttributionResponse?)null);

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
            ContentId = scenarioId,
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
        var returnedStatus = okResult.Value.Should().BeOfType<IpVerificationResponse>().Subject;
        returnedStatus.IsRegistered.Should().BeTrue();
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
            .ReturnsAsync((IpVerificationResponse?)null);

        // Act
        var result = await _controller.GetScenarioIpStatus(scenarioId);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion
}
