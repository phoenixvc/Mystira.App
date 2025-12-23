using FluentAssertions;
using Mystira.App.Application.CQRS.Archetypes.Commands;
using Mystira.App.Application.CQRS.Archetypes.Queries;
using Xunit;

namespace Mystira.App.Application.Tests.CQRS.Archetypes;

public class CreateArchetypeCommandHandlerTests : CqrsIntegrationTestBase
{
    [Fact]
    public async Task Handle_WithValidCommand_CreatesArchetype()
    {
        var command = new CreateArchetypeCommand(
            Name: "The Hero",
            Description: "The archetypal hero character"
        );

        var result = await Mediator.Send(command);

        result.Should().NotBeNull();
        result.Id.Should().NotBeNullOrEmpty();
        result.Name.Should().Be("The Hero");
        result.Description.Should().Be("The archetypal hero character");
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public async Task Handle_WithMissingName_ThrowsArgumentException(string name)
    {
        var command = new CreateArchetypeCommand(
            Name: name,
            Description: "Test description"
        );

        var act = async () => await Mediator.Send(command);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Name is required");
    }

    [Fact]
    public async Task Handle_WithEmptyDescription_CreatesArchetype()
    {
        var command = new CreateArchetypeCommand(
            Name: "The Trickster",
            Description: string.Empty
        );

        var result = await Mediator.Send(command);

        result.Should().NotBeNull();
        result.Name.Should().Be("The Trickster");
        result.Description.Should().Be(string.Empty);
    }

    [Fact]
    public async Task Handle_InvalidatesCachePrefix()
    {
        var command = new CreateArchetypeCommand(
            Name: "The Mentor",
            Description: "The wise mentor archetype"
        );

        await Mediator.Send(command);

        var allArchetypes = await Mediator.Send(new GetAllArchetypesQuery());
        allArchetypes.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_CreatedArchetypePersistsInDatabase()
    {
        var command = new CreateArchetypeCommand(
            Name: "The Shadow",
            Description: "The dark shadow archetype"
        );

        var created = await Mediator.Send(command);

        var retrieved = await DbContext.ArchetypeDefinitions.FindAsync(created.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Name.Should().Be("The Shadow");
        retrieved.Description.Should().Be("The dark shadow archetype");
    }

    [Fact]
    public async Task Handle_WithMultipleArchetypes_CreatesEach()
    {
        var command1 = new CreateArchetypeCommand("Archetype 1", "First archetype");
        var command2 = new CreateArchetypeCommand("Archetype 2", "Second archetype");

        var result1 = await Mediator.Send(command1);
        var result2 = await Mediator.Send(command2);

        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
        result1.Id.Should().NotBe(result2.Id);

        var allArchetypes = await Mediator.Send(new GetAllArchetypesQuery());
        allArchetypes.Should().HaveCount(2);
    }
}
