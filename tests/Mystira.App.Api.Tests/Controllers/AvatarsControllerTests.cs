using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Mystira.App.Api.Controllers;
using Mystira.App.Application.CQRS.Avatars.Queries;
using Mystira.Contracts.App.Responses.Common;
using Mystira.Contracts.App.Responses.Media;
using Wolverine;

namespace Mystira.App.Api.Tests.Controllers;

public class AvatarsControllerTests
{
    private readonly Mock<IMessageBus> _mockBus;
    private readonly Mock<ILogger<AvatarsController>> _mockLogger;
    private readonly AvatarsController _controller;

    public AvatarsControllerTests()
    {
        _mockBus = new Mock<IMessageBus>();
        _mockLogger = new Mock<ILogger<AvatarsController>>();
        _controller = new AvatarsController(_mockBus.Object, _mockLogger.Object);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    #region GetAvatars Tests

    [Fact]
    public async Task GetAvatars_ReturnsOkWithAvatars()
    {
        // Arrange
        var avatarResponse = new AvatarResponse
        {
            AgeGroupAvatars = new Dictionary<string, List<string>>
            {
                { "6-9", new List<string> { "avatar-1", "avatar-2" } }
            }
        };

        _mockBus
            .Setup(x => x.InvokeAsync<AvatarResponse>(
                It.IsAny<GetAvatarsQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(avatarResponse);

        // Act
        var result = await _controller.GetAvatars();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedAvatars = okResult.Value.Should().BeOfType<AvatarResponse>().Subject;
        returnedAvatars.AgeGroupAvatars.Should().ContainKey("6-9");
    }

    [Fact]
    public async Task GetAvatars_WithException_ReturnsInternalServerError()
    {
        // Arrange
        _mockBus
            .Setup(x => x.InvokeAsync<AvatarResponse>(
                It.IsAny<GetAvatarsQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetAvatars();

        // Assert
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region GetAvatarsByAgeGroup Tests

    [Fact]
    public async Task GetAvatarsByAgeGroup_WithValidAgeGroup_ReturnsOk()
    {
        // Arrange
        var ageGroup = "6-9";
        var avatarConfig = new AvatarConfigurationResponse
        {
            AgeGroup = ageGroup,
            AvatarMediaIds = new List<string> { "avatar-1", "avatar-2", "avatar-3" }
        };

        _mockBus
            .Setup(x => x.InvokeAsync<AvatarConfigurationResponse?>(
                It.IsAny<GetAvatarsByAgeGroupQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(avatarConfig);

        // Act
        var result = await _controller.GetAvatarsByAgeGroup(ageGroup);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedConfig = okResult.Value.Should().BeOfType<AvatarConfigurationResponse>().Subject;
        returnedConfig.AgeGroup.Should().Be(ageGroup);
        returnedConfig.AvatarMediaIds.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetAvatarsByAgeGroup_WithEmptyAgeGroup_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.GetAvatarsByAgeGroup("");

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var errorResponse = badRequestResult.Value.Should().BeOfType<ErrorResponse>().Subject;
        errorResponse.Message.Should().Contain("Age group is required");
    }

    [Fact]
    public async Task GetAvatarsByAgeGroup_WithWhitespaceAgeGroup_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.GetAvatarsByAgeGroup("   ");

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetAvatarsByAgeGroup_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        _mockBus
            .Setup(x => x.InvokeAsync<AvatarConfigurationResponse?>(
                It.IsAny<GetAvatarsByAgeGroupQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync((AvatarConfigurationResponse?)null);

        // Act
        var result = await _controller.GetAvatarsByAgeGroup("unknown-age-group");

        // Assert
        var notFoundResult = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var errorResponse = notFoundResult.Value.Should().BeOfType<ErrorResponse>().Subject;
        errorResponse.Message.Should().Contain("No avatars found");
    }

    [Fact]
    public async Task GetAvatarsByAgeGroup_WithException_ReturnsInternalServerError()
    {
        // Arrange
        _mockBus
            .Setup(x => x.InvokeAsync<AvatarConfigurationResponse?>(
                It.IsAny<GetAvatarsByAgeGroupQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetAvatarsByAgeGroup("6-9");

        // Assert
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    [Theory]
    [InlineData("1-2")]
    [InlineData("3-5")]
    [InlineData("6-9")]
    [InlineData("10-12")]
    [InlineData("13-18")]
    [InlineData("19-150")]
    public async Task GetAvatarsByAgeGroup_WithDifferentAgeGroups_CallsQueryWithCorrectParameter(string ageGroup)
    {
        // Arrange
        GetAvatarsByAgeGroupQuery? capturedQuery = null;

        _mockBus
            .Setup(x => x.InvokeAsync<AvatarConfigurationResponse?>(
                It.IsAny<GetAvatarsByAgeGroupQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .Callback<object, CancellationToken, TimeSpan?>((q, ct, ts) => capturedQuery = q as GetAvatarsByAgeGroupQuery)
            .ReturnsAsync(new AvatarConfigurationResponse { AgeGroup = ageGroup, AvatarMediaIds = new List<string>() });

        // Act
        await _controller.GetAvatarsByAgeGroup(ageGroup);

        // Assert
        capturedQuery.Should().NotBeNull();
        capturedQuery!.AgeGroup.Should().Be(ageGroup);
    }

    #endregion
}
