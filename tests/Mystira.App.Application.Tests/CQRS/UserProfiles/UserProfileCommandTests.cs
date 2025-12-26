using FluentAssertions;
using Mystira.App.Application.CQRS.UserProfiles.Commands;
using Mystira.App.Application.CQRS.UserProfiles.Queries;
using Mystira.Contracts.App.Requests.UserProfiles;
using Mystira.App.Domain.Models;
using Xunit;

namespace Mystira.App.Application.Tests.CQRS.UserProfiles;

public class UserProfileCommandTests : CqrsIntegrationTestBase
{
    #region CreateUserProfileCommand Tests

    [Fact]
    public async Task CreateUserProfileCommand_WithValidRequest_CreatesProfile()
    {
        // Arrange
        var request = new CreateUserProfileRequest
        {
            Name = "TestPlayer",
            AgeGroup = "6-9",
            AccountId = "account-1",
            PreferredFantasyThemes = new List<string> { "adventure", "magic" }
        };
        var command = new CreateUserProfileCommand(request);

        // Act
        var result = await Mediator.Send(command);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeNullOrEmpty();
        result.Name.Should().Be("TestPlayer");
        result.AgeGroupName.Should().Be("6-9");
        result.AccountId.Should().Be("account-1");
        result.PreferredFantasyThemes.Should().HaveCount(2);
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Verify persistence
        var savedProfile = await DbContext.UserProfiles.FindAsync(result.Id);
        savedProfile.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateUserProfileCommand_WithDateOfBirth_SetsAgeGroup()
    {
        // Arrange
        var dateOfBirth = DateTime.UtcNow.AddYears(-8); // 8 years old -> 6-9 age group
        var request = new CreateUserProfileRequest
        {
            Name = "ChildPlayer",
            AgeGroup = "6-9",
            DateOfBirth = dateOfBirth
        };
        var command = new CreateUserProfileCommand(request);

        // Act
        var result = await Mediator.Send(command);

        // Assert
        result.Should().NotBeNull();
        result.DateOfBirth.Should().Be(dateOfBirth);
        result.AgeGroupName.Should().Be("6-9");
    }

    [Fact]
    public async Task CreateUserProfileCommand_WithMissingName_ThrowsArgumentException()
    {
        // Arrange
        var request = new CreateUserProfileRequest
        {
            Name = "",
            AgeGroup = "6-9"
        };
        var command = new CreateUserProfileCommand(request);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await Mediator.Send(command);
        });
    }

    [Fact]
    public async Task CreateUserProfileCommand_WithMissingAgeGroup_ThrowsArgumentException()
    {
        // Arrange
        var request = new CreateUserProfileRequest
        {
            Name = "TestPlayer",
            AgeGroup = ""
        };
        var command = new CreateUserProfileCommand(request);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await Mediator.Send(command);
        });
    }

    [Fact]
    public async Task CreateUserProfileCommand_WithInvalidAgeGroup_ThrowsArgumentException()
    {
        // Arrange
        var request = new CreateUserProfileRequest
        {
            Name = "TestPlayer",
            AgeGroup = "invalid-age-group"
        };
        var command = new CreateUserProfileCommand(request);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await Mediator.Send(command);
        });
        exception.Message.Should().Contain("Invalid age group");
    }

    [Fact]
    public async Task CreateUserProfileCommand_AsGuest_CreatesGuestProfile()
    {
        // Arrange
        var request = new CreateUserProfileRequest
        {
            Name = "GuestPlayer",
            AgeGroup = "6-9",
            IsGuest = true
        };
        var command = new CreateUserProfileCommand(request);

        // Act
        var result = await Mediator.Send(command);

        // Assert
        result.Should().NotBeNull();
        result.IsGuest.Should().BeTrue();
        result.AccountId.Should().BeNull();
    }

    [Fact]
    public async Task CreateUserProfileCommand_WithPronouns_SetsPronouns()
    {
        // Arrange
        var request = new CreateUserProfileRequest
        {
            Name = "TestPlayer",
            AgeGroup = "6-9",
            Pronouns = "they/them"
        };
        var command = new CreateUserProfileCommand(request);

        // Act
        var result = await Mediator.Send(command);

        // Assert
        result.Should().NotBeNull();
        result.Pronouns.Should().Be("they/them");
    }

    [Fact]
    public async Task CreateUserProfileCommand_WithBio_SetsBio()
    {
        // Arrange
        var request = new CreateUserProfileRequest
        {
            Name = "TestPlayer",
            AgeGroup = "6-9",
            Bio = "I love adventure stories!"
        };
        var command = new CreateUserProfileCommand(request);

        // Act
        var result = await Mediator.Send(command);

        // Assert
        result.Should().NotBeNull();
        result.Bio.Should().Be("I love adventure stories!");
    }

    [Fact]
    public async Task CreateUserProfileCommand_WithAvatar_SetsAvatarMediaId()
    {
        // Arrange
        var request = new CreateUserProfileRequest
        {
            Name = "TestPlayer",
            AgeGroup = "6-9",
            SelectedAvatarMediaId = "avatar-123"
        };
        var command = new CreateUserProfileCommand(request);

        // Act
        var result = await Mediator.Send(command);

        // Assert
        result.Should().NotBeNull();
        result.SelectedAvatarMediaId.Should().Be("avatar-123");
    }

    #endregion

    #region UpdateUserProfileCommand Tests

    [Fact]
    public async Task UpdateUserProfileCommand_WithValidRequest_UpdatesProfile()
    {
        // Arrange - Create profile first
        var profile = new UserProfile
        {
            Id = "profile-to-update",
            Name = "OriginalName",
            AgeGroupName = "6-9",
            AccountId = "account-1"
        };
        DbContext.UserProfiles.Add(profile);
        await DbContext.SaveChangesAsync();

        var request = new UpdateUserProfileRequest
        {
            AgeGroup = "10-12",
            Bio = "Updated bio",
            Pronouns = "they/them"
        };
        var command = new UpdateUserProfileCommand("profile-to-update", request);

        // Act
        var result = await Mediator.Send(command);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("OriginalName"); // Name doesn't change
        result.AgeGroupName.Should().Be("10-12");
        result.Bio.Should().Be("Updated bio");
        result.Pronouns.Should().Be("they/them");
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task UpdateUserProfileCommand_WithNonExistentProfile_ReturnsNull()
    {
        // Arrange
        var request = new UpdateUserProfileRequest
        {
            Bio = "Test"
        };
        var command = new UpdateUserProfileCommand("non-existent-profile", request);

        // Act
        var result = await Mediator.Send(command);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateUserProfileCommand_WithPartialUpdate_OnlyUpdatesSpecifiedFields()
    {
        // Arrange
        var profile = new UserProfile
        {
            Id = "profile-partial",
            Name = "TestName",
            AgeGroupName = "6-9",
            Bio = "Original bio",
            Pronouns = "she/her"
        };
        DbContext.UserProfiles.Add(profile);
        await DbContext.SaveChangesAsync();

        var request = new UpdateUserProfileRequest
        {
            Bio = "New bio only"
            // Only updating bio, other fields should remain unchanged
        };
        var command = new UpdateUserProfileCommand("profile-partial", request);

        // Act
        var result = await Mediator.Send(command);

        // Assert
        result.Should().NotBeNull();
        result!.Bio.Should().Be("New bio only");
        result.Pronouns.Should().Be("she/her"); // Unchanged
        result.AgeGroupName.Should().Be("6-9"); // Unchanged
    }

    #endregion

    #region DeleteUserProfileCommand Tests

    [Fact]
    public async Task DeleteUserProfileCommand_WithExistingProfile_DeletesProfile()
    {
        // Arrange
        var profile = new UserProfile
        {
            Id = "profile-to-delete",
            Name = "ToDelete",
            AgeGroupName = "6-9"
        };
        DbContext.UserProfiles.Add(profile);
        await DbContext.SaveChangesAsync();

        var command = new DeleteUserProfileCommand("profile-to-delete");

        // Act
        var result = await Mediator.Send(command);

        // Assert
        result.Should().BeTrue();
        var deletedProfile = await DbContext.UserProfiles.FindAsync("profile-to-delete");
        deletedProfile.Should().BeNull();
    }

    [Fact]
    public async Task DeleteUserProfileCommand_WithNonExistentProfile_ReturnsFalse()
    {
        // Arrange
        var command = new DeleteUserProfileCommand("non-existent");

        // Act
        var result = await Mediator.Send(command);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetUserProfileQuery Tests

    [Fact]
    public async Task GetUserProfileQuery_WithExistingProfile_ReturnsProfile()
    {
        // Arrange
        var profile = new UserProfile
        {
            Id = "profile-1",
            Name = "TestProfile",
            AgeGroupName = "6-9",
            AccountId = "account-1"
        };
        DbContext.UserProfiles.Add(profile);
        await DbContext.SaveChangesAsync();

        var query = new GetUserProfileQuery("profile-1");

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("profile-1");
        result.Name.Should().Be("TestProfile");
    }

    [Fact]
    public async Task GetUserProfileQuery_WithNonExistentProfile_ReturnsNull()
    {
        // Arrange
        var query = new GetUserProfileQuery("non-existent");

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetProfilesByAccountQuery Tests

    [Fact]
    public async Task GetProfilesByAccountQuery_WithMultipleProfiles_ReturnsAllProfiles()
    {
        // Arrange
        var profiles = new List<UserProfile>
        {
            new() { Id = "profile-1", Name = "Profile1", AgeGroupName = "6-9", AccountId = "account-1" },
            new() { Id = "profile-2", Name = "Profile2", AgeGroupName = "10-12", AccountId = "account-1" },
            new() { Id = "profile-3", Name = "Profile3", AgeGroupName = "6-9", AccountId = "account-2" }
        };
        DbContext.UserProfiles.AddRange(profiles);
        await DbContext.SaveChangesAsync();

        var query = new GetProfilesByAccountQuery("account-1");

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(p => p.AccountId.Should().Be("account-1"));
    }

    [Fact]
    public async Task GetProfilesByAccountQuery_WithNoProfiles_ReturnsEmptyList()
    {
        // Arrange
        var query = new GetProfilesByAccountQuery("non-existent-account");

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    #endregion
}
