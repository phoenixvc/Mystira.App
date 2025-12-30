using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Mystira.App.Api.Controllers;
using Mystira.App.Application.CQRS.UserProfiles.Commands;
using Mystira.App.Application.CQRS.UserProfiles.Queries;
using Mystira.Domain.Models;
using Mystira.Contracts.App.Requests.UserProfiles;
using Wolverine;
using Xunit;

namespace Mystira.App.Api.Tests.Controllers;

public class UserProfilesControllerTests
{
    private readonly Mock<IMessageBus> _mockBus;
    private readonly Mock<ILogger<UserProfilesController>> _mockLogger;
    private readonly UserProfilesController _controller;

    public UserProfilesControllerTests()
    {
        _mockBus = new Mock<IMessageBus>();
        _mockLogger = new Mock<ILogger<UserProfilesController>>();
        _controller = new UserProfilesController(_mockBus.Object, _mockLogger.Object);

        SetupControllerContext();
    }

    private void SetupControllerContext(string? accountId = "test-account-id")
    {
        var claims = new List<Claim>();
        if (accountId != null)
        {
            claims.Add(new Claim("sub", accountId));
        }

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext
        {
            User = principal,
            TraceIdentifier = "test-trace-id"
        };

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    #region CreateProfile Tests

    [Fact]
    public async Task CreateProfile_WithValidRequest_ReturnsCreatedWithProfile()
    {
        // Arrange
        var request = new CreateUserProfileRequest
        {
            Name = "Test Player",
            AccountId = "account-1",
            AgeGroup = "8-12"
        };
        var createdProfile = new UserProfile { Id = "profile-1", Name = "Test Player" };

        _mockBus
            .Setup(x => x.InvokeAsync<UserProfile>(
                It.IsAny<CreateUserProfileCommand>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(createdProfile);

        // Act
        var result = await _controller.CreateProfile(request);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(UserProfilesController.GetProfileById));
        var returnedProfile = createdResult.Value.Should().BeOfType<UserProfile>().Subject;
        returnedProfile.Name.Should().Be("Test Player");
    }

    [Fact]
    public async Task CreateProfile_WhenArgumentExceptionThrown_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateUserProfileRequest { Name = "Test" };

        _mockBus
            .Setup(x => x.InvokeAsync<UserProfile>(
                It.IsAny<CreateUserProfileCommand>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ThrowsAsync(new ArgumentException("Invalid profile data"));

        // Act
        var result = await _controller.CreateProfile(request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CreateProfile_WhenExceptionThrown_ReturnsInternalServerError()
    {
        // Arrange
        var request = new CreateUserProfileRequest { Name = "Test" };

        _mockBus
            .Setup(x => x.InvokeAsync<UserProfile>(
                It.IsAny<CreateUserProfileCommand>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.CreateProfile(request);

        // Assert
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region GetProfileById Tests

    [Fact]
    public async Task GetProfileById_WhenProfileExists_ReturnsOkWithProfile()
    {
        // Arrange
        var profileId = "profile-1";
        var profile = new UserProfile { Id = profileId, Name = "Test Player" };

        _mockBus
            .Setup(x => x.InvokeAsync<UserProfile?>(
                It.IsAny<GetUserProfileQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(profile);

        // Act
        var result = await _controller.GetProfileById(profileId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedProfile = okResult.Value.Should().BeOfType<UserProfile>().Subject;
        returnedProfile.Id.Should().Be(profileId);
    }

    [Fact]
    public async Task GetProfileById_WhenProfileNotFound_ReturnsNotFound()
    {
        // Arrange
        var profileId = "nonexistent";

        _mockBus
            .Setup(x => x.InvokeAsync<UserProfile?>(
                It.IsAny<GetUserProfileQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(default(UserProfile));

        // Act
        var result = await _controller.GetProfileById(profileId);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetProfileById_WhenExceptionThrown_ReturnsInternalServerError()
    {
        // Arrange
        var profileId = "profile-1";

        _mockBus
            .Setup(x => x.InvokeAsync<UserProfile?>(
                It.IsAny<GetUserProfileQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetProfileById(profileId);

        // Assert
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region GetProfilesByAccount Tests

    [Fact]
    public async Task GetProfilesByAccount_ReturnsOkWithProfiles()
    {
        // Arrange
        var accountId = "account-1";
        var profiles = new List<UserProfile>
        {
            new UserProfile { Id = "profile-1", Name = "Player 1" },
            new UserProfile { Id = "profile-2", Name = "Player 2" }
        };

        _mockBus
            .Setup(x => x.InvokeAsync<List<UserProfile>>(
                It.IsAny<GetProfilesByAccountQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(profiles);

        // Act
        var result = await _controller.GetProfilesByAccount(accountId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedProfiles = okResult.Value.Should().BeOfType<List<UserProfile>>().Subject;
        returnedProfiles.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetProfilesByAccount_WhenExceptionThrown_ReturnsInternalServerError()
    {
        // Arrange
        var accountId = "account-1";

        _mockBus
            .Setup(x => x.InvokeAsync<List<UserProfile>>(
                It.IsAny<GetProfilesByAccountQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetProfilesByAccount(accountId);

        // Assert
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region UpdateProfile Tests

    [Fact]
    public async Task UpdateProfile_WhenProfileExists_ReturnsOkWithUpdatedProfile()
    {
        // Arrange
        var profileId = "profile-1";
        var request = new UpdateUserProfileRequest { Bio = "Updated bio text" };
        var updatedProfile = new UserProfile { Id = profileId, Name = "Updated Name" };

        _mockBus
            .Setup(x => x.InvokeAsync<UserProfile?>(
                It.IsAny<UpdateUserProfileCommand>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(updatedProfile);

        // Act
        var result = await _controller.UpdateProfile(profileId, request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedProfile = okResult.Value.Should().BeOfType<UserProfile>().Subject;
        returnedProfile.Name.Should().Be("Updated Name");
    }

    [Fact]
    public async Task UpdateProfile_WhenProfileNotFound_ReturnsNotFound()
    {
        // Arrange
        var profileId = "nonexistent";
        var request = new UpdateUserProfileRequest { Bio = "Updated bio text" };

        _mockBus
            .Setup(x => x.InvokeAsync<UserProfile?>(
                It.IsAny<UpdateUserProfileCommand>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(default(UserProfile));

        // Act
        var result = await _controller.UpdateProfile(profileId, request);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task UpdateProfile_WhenArgumentExceptionThrown_ReturnsBadRequest()
    {
        // Arrange
        var profileId = "profile-1";
        var request = new UpdateUserProfileRequest { Bio = "" };

        _mockBus
            .Setup(x => x.InvokeAsync<UserProfile?>(
                It.IsAny<UpdateUserProfileCommand>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ThrowsAsync(new ArgumentException("Name cannot be empty"));

        // Act
        var result = await _controller.UpdateProfile(profileId, request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region UpdateProfileById Tests

    [Fact]
    public async Task UpdateProfileById_WhenProfileExists_ReturnsOkWithUpdatedProfile()
    {
        // Arrange
        var profileId = "profile-1";
        var request = new UpdateUserProfileRequest { Bio = "Updated bio text" };
        var updatedProfile = new UserProfile { Id = profileId, Name = "Updated Name" };

        _mockBus
            .Setup(x => x.InvokeAsync<UserProfile?>(
                It.IsAny<UpdateUserProfileCommand>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(updatedProfile);

        // Act
        var result = await _controller.UpdateProfileById(profileId, request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedProfile = okResult.Value.Should().BeOfType<UserProfile>().Subject;
        returnedProfile.Name.Should().Be("Updated Name");
    }

    [Fact]
    public async Task UpdateProfileById_WhenProfileNotFound_ReturnsNotFound()
    {
        // Arrange
        var profileId = "nonexistent";
        var request = new UpdateUserProfileRequest { Bio = "Updated bio text" };

        _mockBus
            .Setup(x => x.InvokeAsync<UserProfile?>(
                It.IsAny<UpdateUserProfileCommand>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(default(UserProfile));

        // Act
        var result = await _controller.UpdateProfileById(profileId, request);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region DeleteProfile Tests

    [Fact]
    public async Task DeleteProfile_WhenProfileExists_ReturnsNoContent()
    {
        // Arrange
        var profileId = "profile-1";

        _mockBus
            .Setup(x => x.InvokeAsync<bool>(
                It.IsAny<DeleteUserProfileCommand>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteProfile(profileId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteProfile_WhenProfileNotFound_ReturnsNotFound()
    {
        // Arrange
        var profileId = "nonexistent";

        _mockBus
            .Setup(x => x.InvokeAsync<bool>(
                It.IsAny<DeleteUserProfileCommand>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteProfile(profileId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DeleteProfile_WhenExceptionThrown_ReturnsInternalServerError()
    {
        // Arrange
        var profileId = "profile-1";

        _mockBus
            .Setup(x => x.InvokeAsync<bool>(
                It.IsAny<DeleteUserProfileCommand>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.DeleteProfile(profileId);

        // Assert
        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region CompleteOnboarding Tests

    [Fact]
    public async Task CompleteOnboarding_WhenProfileExists_ReturnsOk()
    {
        // Arrange
        var profileId = "profile-1";

        _mockBus
            .Setup(x => x.InvokeAsync<bool>(
                It.IsAny<CompleteOnboardingCommand>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.CompleteOnboarding(profileId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task CompleteOnboarding_WhenProfileNotFound_ReturnsNotFound()
    {
        // Arrange
        var profileId = "nonexistent";

        _mockBus
            .Setup(x => x.InvokeAsync<bool>(
                It.IsAny<CompleteOnboardingCommand>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.CompleteOnboarding(profileId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region CreateMultipleProfiles Tests

    [Fact]
    public async Task CreateMultipleProfiles_WithValidRequest_ReturnsOkWithProfiles()
    {
        // Arrange
        var request = new CreateMultipleProfilesRequest
        {
            Profiles = new List<CreateUserProfileRequest>
            {
                new CreateUserProfileRequest { Name = "Player 1", AccountId = "account-1" },
                new CreateUserProfileRequest { Name = "Player 2", AccountId = "account-1" }
            }
        };
        var createdProfiles = new List<UserProfile>
        {
            new UserProfile { Id = "profile-1", Name = "Player 1" },
            new UserProfile { Id = "profile-2", Name = "Player 2" }
        };

        _mockBus
            .Setup(x => x.InvokeAsync<List<UserProfile>>(
                It.IsAny<CreateMultipleProfilesCommand>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(createdProfiles);

        // Act
        var result = await _controller.CreateMultipleProfiles(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedProfiles = okResult.Value.Should().BeOfType<List<UserProfile>>().Subject;
        returnedProfiles.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateMultipleProfiles_WhenExceptionThrown_ReturnsInternalServerError()
    {
        // Arrange
        var request = new CreateMultipleProfilesRequest { Profiles = new List<CreateUserProfileRequest>() };

        _mockBus
            .Setup(x => x.InvokeAsync<List<UserProfile>>(
                It.IsAny<CreateMultipleProfilesCommand>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.CreateMultipleProfiles(request);

        // Assert
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region AssignCharacterToProfile Tests

    [Fact]
    public async Task AssignCharacterToProfile_WithValidRequest_ReturnsOk()
    {
        // Arrange
        var profileId = "profile-1";
        var request = new ProfileAssignmentRequest
        {
            ProfileId = profileId,
            CharacterId = "character-1",
            IsNpcAssignment = false
        };

        _mockBus
            .Setup(x => x.InvokeAsync<bool>(
                It.IsAny<AssignCharacterToProfileCommand>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.AssignCharacterToProfile(profileId, request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task AssignCharacterToProfile_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        var profileId = "nonexistent";
        var request = new ProfileAssignmentRequest
        {
            ProfileId = profileId,
            CharacterId = "character-1"
        };

        _mockBus
            .Setup(x => x.InvokeAsync<bool>(
                It.IsAny<AssignCharacterToProfileCommand>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.AssignCharacterToProfile(profileId, request);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region RemoveProfileFromAccount Tests

    [Fact]
    public async Task RemoveProfileFromAccount_WhenProfileExists_ReturnsOk()
    {
        // Arrange
        var profileId = "profile-1";

        _mockBus
            .Setup(x => x.InvokeAsync<bool>(
                It.IsAny<RemoveProfileFromAccountCommand>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.RemoveProfileFromAccount(profileId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task RemoveProfileFromAccount_WhenProfileNotFound_ReturnsNotFound()
    {
        // Arrange
        var profileId = "nonexistent";

        _mockBus
            .Setup(x => x.InvokeAsync<bool>(
                It.IsAny<RemoveProfileFromAccountCommand>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.RemoveProfileFromAccount(profileId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task RemoveProfileFromAccount_WhenExceptionThrown_ReturnsInternalServerError()
    {
        // Arrange
        var profileId = "profile-1";

        _mockBus
            .Setup(x => x.InvokeAsync<bool>(
                It.IsAny<RemoveProfileFromAccountCommand>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.RemoveProfileFromAccount(profileId);

        // Assert
        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion
}
