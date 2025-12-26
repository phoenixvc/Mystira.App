using FluentAssertions;
using Mystira.App.Application.CQRS.FantasyThemes.Commands;
using Mystira.App.Application.CQRS.FantasyThemes.Queries;
using Mystira.App.Application.CQRS.AgeGroups.Commands;
using Mystira.App.Application.CQRS.AgeGroups.Queries;
using Mystira.App.Domain.Models;
using Xunit;

namespace Mystira.App.Application.Tests.CQRS.MasterData;

public class FantasyThemeAndAgeGroupTests : CqrsIntegrationTestBase
{
    protected override async Task SeedTestDataAsync()
    {
        // Seed FantasyThemes
        var themes = new List<FantasyThemeDefinition>
        {
            new() { Id = "theme-adventure", Name = "Adventure", Description = "Epic quests and journeys" },
            new() { Id = "theme-magic", Name = "Magic", Description = "Spells and sorcery" },
            new() { Id = "theme-nature", Name = "Nature", Description = "Forests and wildlife" }
        };
        DbContext.FantasyThemeDefinitions.AddRange(themes);

        // Seed AgeGroups
        var ageGroups = new List<AgeGroupDefinition>
        {
            new() { Id = "age-6-9", Name = "School Age", Value = "6-9", MinimumAge = 6, MaximumAge = 9, Description = "Ages 6-9" },
            new() { Id = "age-10-12", Name = "Preteens", Value = "10-12", MinimumAge = 10, MaximumAge = 12, Description = "Ages 10-12" },
            new() { Id = "age-13-18", Name = "Teens", Value = "13-18", MinimumAge = 13, MaximumAge = 18, Description = "Ages 13-18" }
        };
        DbContext.AgeGroupDefinitions.AddRange(ageGroups);

        await DbContext.SaveChangesAsync();
    }

    #region FantasyTheme Commands

    [Fact]
    public async Task CreateFantasyThemeCommand_WithValidData_CreatesTheme()
    {
        // Arrange
        var command = new CreateFantasyThemeCommand("Mystery", "Puzzles and secrets");

        // Act
        var result = await Mediator.Send(command);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeNullOrEmpty();
        result.Name.Should().Be("Mystery");
        result.Description.Should().Be("Puzzles and secrets");
    }

    [Fact]
    public async Task CreateFantasyThemeCommand_WithEmptyName_ThrowsArgumentException()
    {
        // Arrange
        var command = new CreateFantasyThemeCommand("", "Description");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await Mediator.Send(command);
        });
    }

    [Fact]
    public async Task UpdateFantasyThemeCommand_WithExistingTheme_UpdatesTheme()
    {
        // Arrange
        await SeedTestDataAsync();
        var command = new UpdateFantasyThemeCommand("theme-adventure", "Quest", "Updated quest description");

        // Act
        var result = await Mediator.Send(command);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Quest");
        result.Description.Should().Contain("Updated");
    }

    [Fact]
    public async Task UpdateFantasyThemeCommand_WithNonExistentTheme_ReturnsNull()
    {
        // Arrange
        await SeedTestDataAsync();
        var command = new UpdateFantasyThemeCommand("non-existent", "Name", "Description");

        // Act
        var result = await Mediator.Send(command);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteFantasyThemeCommand_WithExistingTheme_DeletesTheme()
    {
        // Arrange
        await SeedTestDataAsync();
        var command = new DeleteFantasyThemeCommand("theme-adventure");

        // Act
        var result = await Mediator.Send(command);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteFantasyThemeCommand_WithNonExistentTheme_ReturnsFalse()
    {
        // Arrange
        await SeedTestDataAsync();
        var command = new DeleteFantasyThemeCommand("non-existent");

        // Act
        var result = await Mediator.Send(command);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region FantasyTheme Queries

    [Fact]
    public async Task GetAllFantasyThemesQuery_ReturnsAllThemes()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new GetAllFantasyThemesQuery();

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetFantasyThemeByIdQuery_WithExistingId_ReturnsTheme()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new GetFantasyThemeByIdQuery("theme-magic");

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Magic");
    }

    [Fact]
    public async Task GetFantasyThemeByIdQuery_WithNonExistentId_ReturnsNull()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new GetFantasyThemeByIdQuery("non-existent");

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateFantasyThemeQuery_WithExistingTheme_ReturnsTrue()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new ValidateFantasyThemeQuery("Adventure");

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateFantasyThemeQuery_WithNonExistentTheme_ReturnsFalse()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new ValidateFantasyThemeQuery("NonExistent");

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region AgeGroup Commands

    [Fact]
    public async Task CreateAgeGroupCommand_WithValidData_CreatesAgeGroup()
    {
        // Arrange
        var command = new CreateAgeGroupCommand("Adults", "19+", 19, 99, "Ages 19 and above");

        // Act
        var result = await Mediator.Send(command);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeNullOrEmpty();
        result.Name.Should().Be("Adults");
        result.Value.Should().Be("19+");
        result.MinimumAge.Should().Be(19);
        result.MaximumAge.Should().Be(99);
    }

    [Fact]
    public async Task CreateAgeGroupCommand_WithEmptyName_ThrowsArgumentException()
    {
        // Arrange
        var command = new CreateAgeGroupCommand("", "1-2", 1, 2, "Description");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await Mediator.Send(command);
        });
    }

    [Fact]
    public async Task UpdateAgeGroupCommand_WithExistingGroup_UpdatesGroup()
    {
        // Arrange
        await SeedTestDataAsync();
        var command = new UpdateAgeGroupCommand("age-6-9", "Elementary", "6-9", 6, 9, "Elementary school age");

        // Act
        var result = await Mediator.Send(command);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Elementary");
        result.Description.Should().Contain("Elementary");
    }

    [Fact]
    public async Task DeleteAgeGroupCommand_WithExistingGroup_DeletesGroup()
    {
        // Arrange
        await SeedTestDataAsync();
        var command = new DeleteAgeGroupCommand("age-6-9");

        // Act
        var result = await Mediator.Send(command);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region AgeGroup Queries

    [Fact]
    public async Task GetAllAgeGroupsQuery_ReturnsAllGroups()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new GetAllAgeGroupsQuery();

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetAgeGroupByIdQuery_WithExistingId_ReturnsGroup()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new GetAgeGroupByIdQuery("age-10-12");

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Preteens");
        result.MinimumAge.Should().Be(10);
        result.MaximumAge.Should().Be(12);
    }

    [Fact]
    public async Task ValidateAgeGroupQuery_WithExistingGroup_ReturnsTrue()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new ValidateAgeGroupQuery("6-9");

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAgeGroupQuery_WithNonExistentGroup_ReturnsFalse()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new ValidateAgeGroupQuery("100-200");

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}
