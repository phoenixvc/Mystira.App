using FluentAssertions;
using Mystira.App.Application.CQRS.Archetypes.Commands;
using Mystira.App.Application.CQRS.Archetypes.Queries;
using Mystira.App.Domain.Models;
using Xunit;

namespace Mystira.App.Application.Tests.CQRS.Archetypes;

public class ArchetypeCommandTests : CqrsIntegrationTestBase
{
    protected override async Task SeedTestDataAsync()
    {
        var archetypes = new List<ArchetypeDefinition>
        {
            new()
            {
                Id = "archetype-courage",
                Name = "Courage",
                Description = "The archetype of bravery and facing fears",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = "archetype-wisdom",
                Name = "Wisdom",
                Description = "The archetype of knowledge and understanding",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = "archetype-compassion",
                Name = "Compassion",
                Description = "The archetype of empathy and kindness",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        DbContext.ArchetypeDefinitions.AddRange(archetypes);
        await DbContext.SaveChangesAsync();
    }

    #region CreateArchetypeCommand Tests

    [Fact]
    public async Task CreateArchetypeCommand_WithValidData_CreatesArchetype()
    {
        // Arrange
        var command = new CreateArchetypeCommand("Justice", "The archetype of fairness");

        // Act
        var result = await Mediator.Send(command);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeNullOrEmpty();
        result.Name.Should().Be("Justice");
        result.Description.Should().Be("The archetype of fairness");
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Verify persistence
        var saved = await DbContext.ArchetypeDefinitions.FindAsync(result.Id);
        saved.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateArchetypeCommand_WithEmptyName_ThrowsArgumentException()
    {
        // Arrange
        var command = new CreateArchetypeCommand("", "Description");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await Mediator.Send(command);
        });
    }

    [Fact]
    public async Task CreateArchetypeCommand_WithWhitespaceName_ThrowsArgumentException()
    {
        // Arrange
        var command = new CreateArchetypeCommand("   ", "Description");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await Mediator.Send(command);
        });
    }

    #endregion

    #region UpdateArchetypeCommand Tests

    [Fact]
    public async Task UpdateArchetypeCommand_WithExistingArchetype_UpdatesArchetype()
    {
        // Arrange
        await SeedTestDataAsync();
        var command = new UpdateArchetypeCommand(
            "archetype-courage",
            "Bravery",
            "Updated description about being brave"
        );

        // Act
        var result = await Mediator.Send(command);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Bravery");
        result.Description.Should().Contain("brave");
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task UpdateArchetypeCommand_WithNonExistentArchetype_ReturnsNull()
    {
        // Arrange
        await SeedTestDataAsync();
        var command = new UpdateArchetypeCommand("non-existent", "Name", "Description");

        // Act
        var result = await Mediator.Send(command);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region DeleteArchetypeCommand Tests

    [Fact]
    public async Task DeleteArchetypeCommand_WithExistingArchetype_DeletesArchetype()
    {
        // Arrange
        await SeedTestDataAsync();
        var command = new DeleteArchetypeCommand("archetype-courage");

        // Act
        var result = await Mediator.Send(command);

        // Assert
        result.Should().BeTrue();
        var deleted = await DbContext.ArchetypeDefinitions.FindAsync("archetype-courage");
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteArchetypeCommand_WithNonExistentArchetype_ReturnsFalse()
    {
        // Arrange
        await SeedTestDataAsync();
        var command = new DeleteArchetypeCommand("non-existent");

        // Act
        var result = await Mediator.Send(command);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetAllArchetypesQuery Tests

    [Fact]
    public async Task GetAllArchetypesQuery_ReturnsAllArchetypes()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new GetAllArchetypesQuery();

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetAllArchetypesQuery_WithNoArchetypes_ReturnsEmptyList()
    {
        // Arrange - no seeding
        var query = new GetAllArchetypesQuery();

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    #endregion

    #region GetArchetypeByIdQuery Tests

    [Fact]
    public async Task GetArchetypeByIdQuery_WithExistingId_ReturnsArchetype()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new GetArchetypeByIdQuery("archetype-wisdom");

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Wisdom");
        result.Description.Should().Contain("knowledge");
    }

    [Fact]
    public async Task GetArchetypeByIdQuery_WithNonExistentId_ReturnsNull()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new GetArchetypeByIdQuery("non-existent");

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region ValidateArchetypeQuery Tests

    [Fact]
    public async Task ValidateArchetypeQuery_WithExistingArchetype_ReturnsTrue()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new ValidateArchetypeQuery("Courage");

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateArchetypeQuery_WithNonExistentArchetype_ReturnsFalse()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new ValidateArchetypeQuery("NonExistent");

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateArchetypeQuery_IsCaseInsensitive()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new ValidateArchetypeQuery("COURAGE");

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().BeTrue();
    }

    #endregion
}
