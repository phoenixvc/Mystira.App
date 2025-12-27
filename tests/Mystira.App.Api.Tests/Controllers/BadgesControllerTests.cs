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
using Mystira.App.Application.CQRS.Badges.Queries;
using Mystira.Contracts.App.Responses.Badges;
using CompassAxisScoreResult = Mystira.App.Application.CQRS.Badges.Queries.CompassAxisScoreResult;
using Wolverine;
using Xunit;

namespace Mystira.App.Api.Tests.Controllers;

public class BadgesControllerTests
{
    private readonly Mock<IMessageBus> _mockBus;
    private readonly Mock<ILogger<BadgesController>> _mockLogger;
    private readonly BadgesController _controller;

    public BadgesControllerTests()
    {
        _mockBus = new Mock<IMessageBus>();
        _mockLogger = new Mock<ILogger<BadgesController>>();
        _controller = new BadgesController(_mockBus.Object, _mockLogger.Object);

        // Set up HttpContext for TraceIdentifier
        var httpContext = new DefaultHttpContext();
        httpContext.TraceIdentifier = "test-trace-id";
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    #region GetBadgesByAgeGroup Tests

    [Fact]
    public async Task GetBadgesByAgeGroup_WithValidAgeGroup_ReturnsOkWithBadges()
    {
        // Arrange
        var ageGroup = "8-12";
        var badges = new List<BadgeResponse>
        {
            new BadgeResponse { Id = "badge-1", Title = "Explorer" },
            new BadgeResponse { Id = "badge-2", Title = "Adventurer" }
        };

        _mockBus
            .Setup(x => x.InvokeAsync<List<BadgeResponse>>(
                It.IsAny<GetBadgesByAgeGroupQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(badges);

        // Act
        var result = await _controller.GetBadgesByAgeGroup(ageGroup);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedBadges = okResult.Value.Should().BeOfType<List<BadgeResponse>>().Subject;
        returnedBadges.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetBadgesByAgeGroup_WithEmptyAgeGroup_ReturnsBadRequest()
    {
        // Arrange
        var ageGroup = "";

        // Act
        var result = await _controller.GetBadgesByAgeGroup(ageGroup);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetBadgesByAgeGroup_WithNullAgeGroup_ReturnsBadRequest()
    {
        // Arrange
        string? ageGroup = null;

        // Act
        var result = await _controller.GetBadgesByAgeGroup(ageGroup!);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetBadgesByAgeGroup_WhenExceptionThrown_ReturnsInternalServerError()
    {
        // Arrange
        var ageGroup = "8-12";

        _mockBus
            .Setup(x => x.InvokeAsync<List<BadgeResponse>>(
                It.IsAny<GetBadgesByAgeGroupQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetBadgesByAgeGroup(ageGroup);

        // Assert
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region GetAxisAchievements Tests

    [Fact]
    public async Task GetAxisAchievements_WithValidAgeGroupId_ReturnsOkWithAchievements()
    {
        // Arrange
        var ageGroupId = "age-8-12";
        var achievements = new List<AxisAchievementResponse>
        {
            new AxisAchievementResponse { CompassAxisId = "axis-1", CompassAxisName = "Brave Heart" }
        };

        _mockBus
            .Setup(x => x.InvokeAsync<List<AxisAchievementResponse>>(
                It.IsAny<GetAxisAchievementsQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(achievements);

        // Act
        var result = await _controller.GetAxisAchievements(ageGroupId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedAchievements = okResult.Value.Should().BeOfType<List<AxisAchievementResponse>>().Subject;
        returnedAchievements.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetAxisAchievements_WithEmptyAgeGroupId_ReturnsBadRequest()
    {
        // Arrange
        var ageGroupId = "";

        // Act
        var result = await _controller.GetAxisAchievements(ageGroupId);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetAxisAchievements_WhenExceptionThrown_ReturnsInternalServerError()
    {
        // Arrange
        var ageGroupId = "age-8-12";

        _mockBus
            .Setup(x => x.InvokeAsync<List<AxisAchievementResponse>>(
                It.IsAny<GetAxisAchievementsQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetAxisAchievements(ageGroupId);

        // Assert
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region GetBadgeDetail Tests

    [Fact]
    public async Task GetBadgeDetail_WhenBadgeExists_ReturnsOkWithBadge()
    {
        // Arrange
        var badgeId = "badge-1";
        var badge = new BadgeResponse { Id = badgeId, Title = "Explorer" };

        _mockBus
            .Setup(x => x.InvokeAsync<BadgeResponse?>(
                It.IsAny<GetBadgeDetailQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(badge);

        // Act
        var result = await _controller.GetBadgeDetail(badgeId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedBadge = okResult.Value.Should().BeOfType<BadgeResponse>().Subject;
        returnedBadge.Id.Should().Be(badgeId);
    }

    [Fact]
    public async Task GetBadgeDetail_WhenBadgeNotFound_ReturnsNotFound()
    {
        // Arrange
        var badgeId = "nonexistent";

        _mockBus
            .Setup(x => x.InvokeAsync<BadgeResponse?>(
                It.IsAny<GetBadgeDetailQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(default(BadgeResponse));

        // Act
        var result = await _controller.GetBadgeDetail(badgeId);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetBadgeDetail_WhenExceptionThrown_ReturnsInternalServerError()
    {
        // Arrange
        var badgeId = "badge-1";

        _mockBus
            .Setup(x => x.InvokeAsync<BadgeResponse?>(
                It.IsAny<GetBadgeDetailQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetBadgeDetail(badgeId);

        // Assert
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region GetProfileBadgeProgress Tests

    [Fact]
    public async Task GetProfileBadgeProgress_WhenProfileExists_ReturnsOkWithProgress()
    {
        // Arrange
        var profileId = "profile-1";
        var progress = new BadgeProgressResponse { AgeGroupId = "8-12" };

        _mockBus
            .Setup(x => x.InvokeAsync<BadgeProgressResponse?>(
                It.IsAny<GetProfileBadgeProgressQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(progress);

        // Act
        var result = await _controller.GetProfileBadgeProgress(profileId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedProgress = okResult.Value.Should().BeOfType<BadgeProgressResponse>().Subject;
        returnedProgress.AgeGroupId.Should().Be("8-12");
    }

    [Fact]
    public async Task GetProfileBadgeProgress_WhenProfileNotFound_ReturnsNotFound()
    {
        // Arrange
        var profileId = "nonexistent";

        _mockBus
            .Setup(x => x.InvokeAsync<BadgeProgressResponse?>(
                It.IsAny<GetProfileBadgeProgressQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(default(BadgeProgressResponse));

        // Act
        var result = await _controller.GetProfileBadgeProgress(profileId);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetProfileBadgeProgress_WhenExceptionThrown_ReturnsInternalServerError()
    {
        // Arrange
        var profileId = "profile-1";

        _mockBus
            .Setup(x => x.InvokeAsync<BadgeProgressResponse?>(
                It.IsAny<GetProfileBadgeProgressQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetProfileBadgeProgress(profileId);

        // Assert
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region CalculateBadgeScores Tests

    [Fact]
    public async Task CalculateBadgeScores_WithValidRequest_ReturnsOkWithResults()
    {
        // Arrange
        var request = new CalculateBadgeScoresRequest
        {
            ContentBundleId = "bundle-1",
            Percentiles = new List<double> { 50, 75, 90, 95 }
        };
        var results = new List<CompassAxisScoreResult>
        {
            new CompassAxisScoreResult { AxisName = "Courage" }
        };

        _mockBus
            .Setup(x => x.InvokeAsync<List<CompassAxisScoreResult>>(
                It.IsAny<CalculateBadgeScoresQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(results);

        // Act
        var result = await _controller.CalculateBadgeScores(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeOfType<List<CompassAxisScoreResult>>();
    }

    [Fact]
    public async Task CalculateBadgeScores_WithNullRequest_ReturnsBadRequest()
    {
        // Arrange
        CalculateBadgeScoresRequest? request = null;

        // Act
        var result = await _controller.CalculateBadgeScores(request!);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CalculateBadgeScores_WithEmptyContentBundleId_ReturnsBadRequest()
    {
        // Arrange
        var request = new CalculateBadgeScoresRequest
        {
            ContentBundleId = "",
            Percentiles = new List<double> { 50, 75 }
        };

        // Act
        var result = await _controller.CalculateBadgeScores(request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CalculateBadgeScores_WithEmptyPercentiles_ReturnsBadRequest()
    {
        // Arrange
        var request = new CalculateBadgeScoresRequest
        {
            ContentBundleId = "bundle-1",
            Percentiles = new List<double>()
        };

        // Act
        var result = await _controller.CalculateBadgeScores(request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CalculateBadgeScores_WithNullPercentiles_ReturnsBadRequest()
    {
        // Arrange
        var request = new CalculateBadgeScoresRequest
        {
            ContentBundleId = "bundle-1",
            Percentiles = null!
        };

        // Act
        var result = await _controller.CalculateBadgeScores(request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CalculateBadgeScores_WhenArgumentExceptionThrown_ReturnsBadRequest()
    {
        // Arrange
        var request = new CalculateBadgeScoresRequest
        {
            ContentBundleId = "bundle-1",
            Percentiles = new List<double> { 50, 75 }
        };

        _mockBus
            .Setup(x => x.InvokeAsync<List<CompassAxisScoreResult>>(
                It.IsAny<CalculateBadgeScoresQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ThrowsAsync(new ArgumentException("Invalid percentile value"));

        // Act
        var result = await _controller.CalculateBadgeScores(request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CalculateBadgeScores_WhenBundleNotFound_ReturnsNotFound()
    {
        // Arrange
        var request = new CalculateBadgeScoresRequest
        {
            ContentBundleId = "nonexistent",
            Percentiles = new List<double> { 50, 75 }
        };

        _mockBus
            .Setup(x => x.InvokeAsync<List<CompassAxisScoreResult>>(
                It.IsAny<CalculateBadgeScoresQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ThrowsAsync(new InvalidOperationException("Content bundle not found"));

        // Act
        var result = await _controller.CalculateBadgeScores(request);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task CalculateBadgeScores_WhenExceptionThrown_ReturnsInternalServerError()
    {
        // Arrange
        var request = new CalculateBadgeScoresRequest
        {
            ContentBundleId = "bundle-1",
            Percentiles = new List<double> { 50, 75 }
        };

        _mockBus
            .Setup(x => x.InvokeAsync<List<CompassAxisScoreResult>>(
                It.IsAny<CalculateBadgeScoresQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.CalculateBadgeScores(request);

        // Assert
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion
}
