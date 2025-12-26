using FluentAssertions;
using Mystira.App.Application.CQRS.Archetypes.Commands;
using Mystira.App.Application.CQRS.Archetypes.Queries;
using Mystira.App.Domain.Models;
using Xunit;

namespace Mystira.App.Application.Tests.CQRS.Archetypes;

public class UpdateArchetypeCommandHandlerTests : CqrsIntegrationTestBase
{
    protected override async Task SeedTestDataAsync()
    {
        var archetype = new ArchetypeDefinition
        {
            Id = "arch-1",
            Name = "The Hero",
            Description = "The archetypal hero",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await DbContext.ArchetypeDefinitions.AddAsync(archetype);
        await DbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task Handle_WithValidCommand_UpdatesArchetype()
    {
        await SeedTestDataAsync();

        var command = new UpdateArchetypeCommand(
            Id: "arch-1",
            Name: "The Brave Hero",
            Description: "Updated hero description"
        );

        var result = await Mediator.Send(command);

        result.Should().NotBeNull();
        result!.Id.Should().Be("arch-1");
        result.Name.Should().Be("The Brave Hero");
        result.Description.Should().Be("Updated hero description");
    }

    [Fact]
    public async Task Handle_WithNonExistentId_ReturnsNull()
    {
        var command = new UpdateArchetypeCommand(
            Id: "invalid-id",
            Name: "The Trickster",
            Description: "Trickster archetype"
        );

        var result = await Mediator.Send(command);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_UpdatesPersistedEntity()
    {
        await SeedTestDataAsync();

        var command = new UpdateArchetypeCommand(
            Id: "arch-1",
            Name: "The Mentor",
            Description: "The wise mentor archetype"
        );

        await Mediator.Send(command);

        var updated = await DbContext.ArchetypeDefinitions.FindAsync("arch-1");
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("The Mentor");
        updated.Description.Should().Be("The wise mentor archetype");
    }

    [Fact]
    public async Task Handle_UpdatesTimestamp()
    {
        await SeedTestDataAsync();
        var originalArchetype = await DbContext.ArchetypeDefinitions.FindAsync("arch-1");
        var originalUpdateTime = originalArchetype!.UpdatedAt;

        await Task.Delay(100);

        var command = new UpdateArchetypeCommand(
            Id: "arch-1",
            Name: "Updated",
            Description: "Updated description"
        );

        var result = await Mediator.Send(command);

        result!.UpdatedAt.Should().BeAfter(originalUpdateTime);
    }

    [Fact]
    public async Task Handle_InvalidatesCachePrefix()
    {
        await SeedTestDataAsync();
        var initialArchetypes = await Mediator.Send(new GetAllArchetypesQuery());

        var command = new UpdateArchetypeCommand(
            Id: "arch-1",
            Name: "Modified",
            Description: "Modified description"
        );

        await Mediator.Send(command);
        var updatedArchetypes = await Mediator.Send(new GetAllArchetypesQuery());

        updatedArchetypes.Should().HaveCount(initialArchetypes.Count);
        updatedArchetypes.Should().Contain(arch => arch.Name == "Modified");
    }

    [Fact]
    public async Task Handle_WithEmptyDescription_UpdatesWithEmpty()
    {
        await SeedTestDataAsync();

        var command = new UpdateArchetypeCommand(
            Id: "arch-1",
            Name: "Updated",
            Description: string.Empty
        );

        var result = await Mediator.Send(command);

        result!.Description.Should().Be(string.Empty);
    }
}
