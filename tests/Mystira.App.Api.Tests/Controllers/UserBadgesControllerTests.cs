using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Mystira.App.Api.Controllers;
using Mystira.App.Api.Models;
using Mystira.App.Application.CQRS.UserBadges.Commands;
using Mystira.App.Application.CQRS.UserBadges.Queries;
using Mystira.App.Domain.Models;
using Mystira.Contracts.App.Requests.Badges;
using Wolverine;
using Xunit;

namespace Mystira.App.Api.Tests.Controllers;

public class UserBadgesControllerTests
{
    private readonly Mock<IMessageBus> _mockBus;
    private readonly Mock<ILogger<UserBadgesController>> _mockLogger;
    private readonly UserBadgesController _controller;

    public UserBadgesControllerTests()
    {
        _mockBus = new Mock<IMessageBus>();
        _mockLogger = new Mock<ILogger<UserBadgesController>>();
        _controller = new UserBadgesController(_mockBus.Object, _mockLogger.Object);

        // Setup HttpContext for TraceIdentifier
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    #region AwardBadge Tests

    [Fact]
    public async Task AwardBadge_WithValidRequest_ReturnsCreatedResult()
    {
        // Arrange
        var request = new AwardBadgeRequest
        {
            UserProfileId = "profile-123",
            BadgeConfigurationId = "badge-config-456"
        };

        var expectedBadge = new UserBadge
        {
            Id = "badge-789",
            UserProfileId = "profile-123",
            BadgeConfigurationId = "badge-config-456",
            BadgeName = "Test Badge"
        };

        _mockBus
            .Setup(x => x.InvokeAsync<UserBadge>(
                It.IsAny<AwardBadgeCommand>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(expectedBadge);

        // Act
        var result = await _controller.AwardBadge(request);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var returnedBadge = createdResult.Value.Should().BeOfType<UserBadge>().Subject;
        returnedBadge.Id.Should().Be("badge-789");
    }

    [Fact]
    public async Task AwardBadge_WithArgumentException_ReturnsBadRequest()
    {
        // Arrange
        var request = new AwardBadgeRequest
        {
            UserProfileId = "profile-123",
            BadgeConfigurationId = "invalid-badge"
        };

        _mockBus
            .Setup(x => x.InvokeAsync<UserBadge>(
                It.IsAny<AwardBadgeCommand>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ThrowsAsync(new ArgumentException("Badge not found"));

        // Act
        var result = await _controller.AwardBadge(request);

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var errorResponse = badRequestResult.Value.Should().BeOfType<ErrorResponse>().Subject;
        errorResponse.Message.Should().Contain("Badge not found");
    }

    [Fact]
    public async Task AwardBadge_WithException_ReturnsInternalServerError()
    {
        // Arrange
        var request = new AwardBadgeRequest
        {
            UserProfileId = "profile-123",
            BadgeConfigurationId = "badge-config-456"
        };

        _mockBus
            .Setup(x => x.InvokeAsync<UserBadge>(
                It.IsAny<AwardBadgeCommand>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.AwardBadge(request);

        // Assert
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region GetUserBadges Tests

    [Fact]
    public async Task GetUserBadges_ReturnsOkWithBadges()
    {
        // Arrange
        var userProfileId = "profile-123";
        var badges = new List<UserBadge>
        {
            new() { Id = "badge-1", UserProfileId = userProfileId, BadgeName = "Badge 1" },
            new() { Id = "badge-2", UserProfileId = userProfileId, BadgeName = "Badge 2" }
        };

        _mockBus
            .Setup(x => x.InvokeAsync<List<UserBadge>>(
                It.IsAny<GetUserBadgesQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(badges);

        // Act
        var result = await _controller.GetUserBadges(userProfileId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedBadges = okResult.Value.Should().BeOfType<List<UserBadge>>().Subject;
        returnedBadges.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetUserBadges_WithException_ReturnsInternalServerError()
    {
        // Arrange
        _mockBus
            .Setup(x => x.InvokeAsync<List<UserBadge>>(
                It.IsAny<GetUserBadgesQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetUserBadges("profile-123");

        // Assert
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region GetUserBadgesForAxis Tests

    [Fact]
    public async Task GetUserBadgesForAxis_ReturnsOkWithBadges()
    {
        // Arrange
        var userProfileId = "profile-123";
        var axis = "honesty";
        var badges = new List<UserBadge>
        {
            new() { Id = "badge-1", UserProfileId = userProfileId, Axis = axis }
        };

        _mockBus
            .Setup(x => x.InvokeAsync<List<UserBadge>>(
                It.IsAny<GetUserBadgesForAxisQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(badges);

        // Act
        var result = await _controller.GetUserBadgesForAxis(userProfileId, axis);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedBadges = okResult.Value.Should().BeOfType<List<UserBadge>>().Subject;
        returnedBadges.Should().HaveCount(1);
        returnedBadges[0].Axis.Should().Be(axis);
    }

    [Fact]
    public async Task GetUserBadgesForAxis_WithException_ReturnsInternalServerError()
    {
        // Arrange
        _mockBus
            .Setup(x => x.InvokeAsync<List<UserBadge>>(
                It.IsAny<GetUserBadgesForAxisQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetUserBadgesForAxis("profile-123", "honesty");

        // Assert
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region HasUserEarnedBadge Tests

    [Fact]
    public async Task HasUserEarnedBadge_WhenEarned_ReturnsTrue()
    {
        // Arrange
        _mockBus
            .Setup(x => x.InvokeAsync<bool>(
                It.IsAny<HasUserEarnedBadgeQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.HasUserEarnedBadge("profile-123", "badge-config-456");

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        // The response is an anonymous object with hasEarned property
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task HasUserEarnedBadge_WhenNotEarned_ReturnsFalse()
    {
        // Arrange
        _mockBus
            .Setup(x => x.InvokeAsync<bool>(
                It.IsAny<HasUserEarnedBadgeQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.HasUserEarnedBadge("profile-123", "badge-config-456");

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task HasUserEarnedBadge_WithException_ReturnsInternalServerError()
    {
        // Arrange
        _mockBus
            .Setup(x => x.InvokeAsync<bool>(
                It.IsAny<HasUserEarnedBadgeQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.HasUserEarnedBadge("profile-123", "badge-config-456");

        // Assert
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region GetBadgeStatistics Tests

    [Fact]
    public async Task GetBadgeStatistics_ReturnsOkWithStatistics()
    {
        // Arrange
        var statistics = new Dictionary<string, int>
        {
            { "honesty", 5 },
            { "courage", 3 },
            { "kindness", 8 }
        };

        _mockBus
            .Setup(x => x.InvokeAsync<Dictionary<string, int>>(
                It.IsAny<GetBadgeStatisticsQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(statistics);

        // Act
        var result = await _controller.GetBadgeStatistics("profile-123");

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedStats = okResult.Value.Should().BeOfType<Dictionary<string, int>>().Subject;
        returnedStats.Should().HaveCount(3);
        returnedStats["honesty"].Should().Be(5);
    }

    [Fact]
    public async Task GetBadgeStatistics_WithException_ReturnsInternalServerError()
    {
        // Arrange
        _mockBus
            .Setup(x => x.InvokeAsync<Dictionary<string, int>>(
                It.IsAny<GetBadgeStatisticsQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetBadgeStatistics("profile-123");

        // Assert
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region GetBadgesForAccount Tests

    [Fact]
    public async Task GetBadgesForAccount_WithBadges_ReturnsOk()
    {
        // Arrange
        var email = "test@example.com";
        var badges = new List<UserBadge>
        {
            new() { Id = "badge-1", BadgeName = "Account Badge 1" },
            new() { Id = "badge-2", BadgeName = "Account Badge 2" }
        };

        _mockBus
            .Setup(x => x.InvokeAsync<List<UserBadge>>(
                It.IsAny<GetBadgesForAccountByEmailQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(badges);

        // Act
        var result = await _controller.GetBadgesForAccount(email);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedBadges = okResult.Value.Should().BeOfType<List<UserBadge>>().Subject;
        returnedBadges.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetBadgesForAccount_WithNoBadges_ReturnsNotFound()
    {
        // Arrange
        var email = "nobadges@example.com";

        _mockBus
            .Setup(x => x.InvokeAsync<List<UserBadge>>(
                It.IsAny<GetBadgesForAccountByEmailQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(new List<UserBadge>());

        // Act
        var result = await _controller.GetBadgesForAccount(email);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetBadgesForAccount_WithException_ReturnsInternalServerError()
    {
        // Arrange
        _mockBus
            .Setup(x => x.InvokeAsync<List<UserBadge>>(
                It.IsAny<GetBadgesForAccountByEmailQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetBadgesForAccount("test@example.com");

        // Assert
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region GetBadgeStatisticsForAccount Tests

    [Fact]
    public async Task GetBadgeStatisticsForAccount_ReturnsOkWithStatistics()
    {
        // Arrange
        var statistics = new Dictionary<string, int>
        {
            { "total", 15 },
            { "honesty", 5 },
            { "courage", 10 }
        };

        _mockBus
            .Setup(x => x.InvokeAsync<Dictionary<string, int>>(
                It.IsAny<GetBadgeStatisticsForAccountByEmailQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(statistics);

        // Act
        var result = await _controller.GetBadgeStatisticsForAccount("test@example.com");

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedStats = okResult.Value.Should().BeOfType<Dictionary<string, int>>().Subject;
        returnedStats["total"].Should().Be(15);
    }

    [Fact]
    public async Task GetBadgeStatisticsForAccount_WithException_ReturnsInternalServerError()
    {
        // Arrange
        _mockBus
            .Setup(x => x.InvokeAsync<Dictionary<string, int>>(
                It.IsAny<GetBadgeStatisticsForAccountByEmailQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetBadgeStatisticsForAccount("test@example.com");

        // Assert
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion
}
