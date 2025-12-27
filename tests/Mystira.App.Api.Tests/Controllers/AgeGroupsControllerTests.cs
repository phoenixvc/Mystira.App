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
using Mystira.App.Api.Models;
using Mystira.App.Application.CQRS.AgeGroups.Queries;
using Mystira.App.Domain.Models;
using Wolverine;
using Xunit;

namespace Mystira.App.Api.Tests.Controllers;

public class AgeGroupsControllerTests
{
    private readonly Mock<IMessageBus> _mockBus;
    private readonly Mock<ILogger<AgeGroupsController>> _mockLogger;
    private readonly AgeGroupsController _controller;

    public AgeGroupsControllerTests()
    {
        _mockBus = new Mock<IMessageBus>();
        _mockLogger = new Mock<ILogger<AgeGroupsController>>();
        _controller = new AgeGroupsController(_mockBus.Object, _mockLogger.Object);

        // Setup HttpContext for TraceIdentifier
        var httpContext = new DefaultHttpContext();
        httpContext.TraceIdentifier = "test-trace-id";
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    #region GetAllAgeGroups Tests

    [Fact]
    public async Task GetAllAgeGroups_ReturnsOkWithAgeGroups()
    {
        // Arrange
        var ageGroups = new List<AgeGroupDefinition>
        {
            new AgeGroupDefinition { Id = "kids", Name = "Kids", MinimumAge = 5, MaximumAge = 12 },
            new AgeGroupDefinition { Id = "teens", Name = "Teens", MinimumAge = 13, MaximumAge = 17 },
            new AgeGroupDefinition { Id = "adults", Name = "Adults", MinimumAge = 18, MaximumAge = 99 }
        };

        _mockBus
            .Setup(x => x.InvokeAsync<List<AgeGroupDefinition>>(
                It.IsAny<GetAllAgeGroupsQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(ageGroups);

        // Act
        var result = await _controller.GetAllAgeGroups();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedAgeGroups = okResult.Value.Should().BeOfType<List<AgeGroupDefinition>>().Subject;
        returnedAgeGroups.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetAllAgeGroups_WhenNoAgeGroups_ReturnsEmptyList()
    {
        // Arrange
        _mockBus
            .Setup(x => x.InvokeAsync<List<AgeGroupDefinition>>(
                It.IsAny<GetAllAgeGroupsQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(new List<AgeGroupDefinition>());

        // Act
        var result = await _controller.GetAllAgeGroups();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedAgeGroups = okResult.Value.Should().BeOfType<List<AgeGroupDefinition>>().Subject;
        returnedAgeGroups.Should().BeEmpty();
    }

    #endregion

    #region GetAgeGroupById Tests

    [Fact]
    public async Task GetAgeGroupById_WhenAgeGroupExists_ReturnsOkWithAgeGroup()
    {
        // Arrange
        var ageGroupId = "kids";
        var ageGroup = new AgeGroupDefinition { Id = ageGroupId, Name = "Kids", MinimumAge = 5, MaximumAge = 12 };

        _mockBus
            .Setup(x => x.InvokeAsync<AgeGroupDefinition?>(
                It.IsAny<GetAgeGroupByIdQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(ageGroup);

        // Act
        var result = await _controller.GetAgeGroupById(ageGroupId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedAgeGroup = okResult.Value.Should().BeOfType<AgeGroupDefinition>().Subject;
        returnedAgeGroup.Id.Should().Be(ageGroupId);
        returnedAgeGroup.Name.Should().Be("Kids");
    }

    [Fact]
    public async Task GetAgeGroupById_WhenAgeGroupNotFound_ReturnsNotFound()
    {
        // Arrange
        var ageGroupId = "nonexistent";

        _mockBus
            .Setup(x => x.InvokeAsync<AgeGroupDefinition?>(
                It.IsAny<GetAgeGroupByIdQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync((AgeGroupDefinition?)null);

        // Act
        var result = await _controller.GetAgeGroupById(ageGroupId);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetAgeGroupById_VerifiesCorrectQueryIsSent()
    {
        // Arrange
        var ageGroupId = "teens";
        var ageGroup = new AgeGroupDefinition { Id = ageGroupId, Name = "Teens" };

        _mockBus
            .Setup(x => x.InvokeAsync<AgeGroupDefinition?>(
                It.Is<GetAgeGroupByIdQuery>(q => q.Id == ageGroupId),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(ageGroup);

        // Act
        await _controller.GetAgeGroupById(ageGroupId);

        // Assert
        _mockBus.Verify(x => x.InvokeAsync<AgeGroupDefinition?>(
            It.Is<GetAgeGroupByIdQuery>(q => q.Id == ageGroupId),
            It.IsAny<CancellationToken>(),
            It.IsAny<TimeSpan?>()), Times.Once);
    }

    #endregion

    #region ValidateAgeGroup Tests

    [Fact]
    public async Task ValidateAgeGroup_WhenValid_ReturnsOkWithValidResult()
    {
        // Arrange
        var request = new ValidateAgeGroupRequest { Value = "kids" };

        _mockBus
            .Setup(x => x.InvokeAsync<bool>(
                It.IsAny<ValidateAgeGroupQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.ValidateAgeGroup(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var validationResult = okResult.Value.Should().BeOfType<ValidationResult>().Subject;
        validationResult.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAgeGroup_WhenInvalid_ReturnsOkWithInvalidResult()
    {
        // Arrange
        var request = new ValidateAgeGroupRequest { Value = "invalid-age-group" };

        _mockBus
            .Setup(x => x.InvokeAsync<bool>(
                It.IsAny<ValidateAgeGroupQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.ValidateAgeGroup(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var validationResult = okResult.Value.Should().BeOfType<ValidationResult>().Subject;
        validationResult.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAgeGroup_VerifiesCorrectValueIsSent()
    {
        // Arrange
        var request = new ValidateAgeGroupRequest { Value = "adults" };

        _mockBus
            .Setup(x => x.InvokeAsync<bool>(
                It.Is<ValidateAgeGroupQuery>(q => q.Value == "adults"),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(true);

        // Act
        await _controller.ValidateAgeGroup(request);

        // Assert
        _mockBus.Verify(x => x.InvokeAsync<bool>(
            It.Is<ValidateAgeGroupQuery>(q => q.Value == "adults"),
            It.IsAny<CancellationToken>(),
            It.IsAny<TimeSpan?>()), Times.Once);
    }

    #endregion
}
