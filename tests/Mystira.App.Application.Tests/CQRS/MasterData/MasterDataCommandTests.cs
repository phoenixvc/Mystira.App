using FluentAssertions;
using Mystira.App.Application.CQRS.CompassAxes.Commands;
using Mystira.App.Application.CQRS.CompassAxes.Queries;
using Mystira.App.Application.CQRS.EchoTypes.Commands;
using Mystira.App.Application.CQRS.EchoTypes.Queries;
using Mystira.App.Domain.Models;
using Xunit;

namespace Mystira.App.Application.Tests.CQRS.MasterData;

public class MasterDataCommandTests : CqrsIntegrationTestBase
{
    protected override async Task SeedTestDataAsync()
    {
        // Seed CompassAxes
        var compassAxes = new List<CompassAxis>
        {
            new() { Id = "axis-courage", Name = "Courage", Description = "Facing fears" },
            new() { Id = "axis-wisdom", Name = "Wisdom", Description = "Seeking knowledge" },
            new() { Id = "axis-compassion", Name = "Compassion", Description = "Showing empathy" }
        };
        DbContext.CompassAxes.AddRange(compassAxes);

        // Seed EchoTypes
        var echoTypes = new List<EchoTypeDefinition>
        {
            new() { Id = "echo-honesty", Name = "Honesty", Description = "Truth telling", Category = "moral" },
            new() { Id = "echo-bravery", Name = "Bravery", Description = "Facing danger", Category = "behavioral" },
            new() { Id = "echo-kindness", Name = "Kindness", Description = "Being kind", Category = "social" }
        };
        DbContext.EchoTypeDefinitions.AddRange(echoTypes);

        await DbContext.SaveChangesAsync();
    }

    #region CompassAxis Commands

    [Fact]
    public async Task CreateCompassAxisCommand_WithValidData_CreatesAxis()
    {
        // Arrange
        var command = new CreateCompassAxisCommand("Justice", "Fairness and equity");

        // Act
        var result = await Mediator.Send(command);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeNullOrEmpty();
        result.Name.Should().Be("Justice");
        result.Description.Should().Be("Fairness and equity");
    }

    [Fact]
    public async Task CreateCompassAxisCommand_WithEmptyName_ThrowsArgumentException()
    {
        // Arrange
        var command = new CreateCompassAxisCommand("", "Description");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await Mediator.Send(command);
        });
    }

    [Fact]
    public async Task UpdateCompassAxisCommand_WithExistingAxis_UpdatesAxis()
    {
        // Arrange
        await SeedTestDataAsync();
        var command = new UpdateCompassAxisCommand("axis-courage", "Bravery", "Updated bravery description");

        // Act
        var result = await Mediator.Send(command);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Bravery");
        result.Description.Should().Contain("bravery");
    }

    [Fact]
    public async Task DeleteCompassAxisCommand_WithExistingAxis_DeletesAxis()
    {
        // Arrange
        await SeedTestDataAsync();
        var command = new DeleteCompassAxisCommand("axis-courage");

        // Act
        var result = await Mediator.Send(command);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region CompassAxis Queries

    [Fact]
    public async Task GetAllCompassAxesQuery_ReturnsAllAxes()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new GetAllCompassAxesQuery();

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetCompassAxisByIdQuery_WithExistingId_ReturnsAxis()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new GetCompassAxisByIdQuery("axis-wisdom");

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Wisdom");
    }

    [Fact]
    public async Task GetCompassAxisByIdQuery_WithNonExistentId_ReturnsNull()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new GetCompassAxisByIdQuery("non-existent");

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateCompassAxisQuery_WithExistingAxis_ReturnsTrue()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new ValidateCompassAxisQuery("Courage");

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateCompassAxisQuery_WithNonExistentAxis_ReturnsFalse()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new ValidateCompassAxisQuery("NonExistent");

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region EchoType Commands

    [Fact]
    public async Task CreateEchoTypeCommand_WithValidData_CreatesEchoType()
    {
        // Arrange
        var command = new CreateEchoTypeCommand("Trust", "Building trust");

        // Act
        var result = await Mediator.Send(command);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeNullOrEmpty();
        result.Name.Should().Be("Trust");
    }

    [Fact]
    public async Task CreateEchoTypeCommand_WithEmptyName_ThrowsArgumentException()
    {
        // Arrange
        var command = new CreateEchoTypeCommand("", "Description");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await Mediator.Send(command);
        });
    }

    [Fact]
    public async Task UpdateEchoTypeCommand_WithExistingType_UpdatesType()
    {
        // Arrange
        await SeedTestDataAsync();
        var command = new UpdateEchoTypeCommand("echo-honesty", "Truthfulness", "Speaking truth");

        // Act
        var result = await Mediator.Send(command);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Truthfulness");
    }

    [Fact]
    public async Task DeleteEchoTypeCommand_WithExistingType_DeletesType()
    {
        // Arrange
        await SeedTestDataAsync();
        var command = new DeleteEchoTypeCommand("echo-honesty");

        // Act
        var result = await Mediator.Send(command);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region EchoType Queries

    [Fact]
    public async Task GetAllEchoTypesQuery_ReturnsAllTypes()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new GetAllEchoTypesQuery();

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetEchoTypeByIdQuery_WithExistingId_ReturnsType()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new GetEchoTypeByIdQuery("echo-bravery");

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Bravery");
        result.Category.Should().Be("behavioral");
    }

    [Fact]
    public async Task ValidateEchoTypeQuery_WithExistingType_ReturnsTrue()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new ValidateEchoTypeQuery("Honesty");

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateEchoTypeQuery_WithNonExistentType_ReturnsFalse()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new ValidateEchoTypeQuery("NonExistent");

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}
