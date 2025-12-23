using FluentAssertions;
using Mystira.App.Application.CQRS.Archetypes.Commands;
using Mystira.App.Application.CQRS.Archetypes.Queries;
using Mystira.App.Domain.Models;
using Xunit;

namespace Mystira.App.Application.Tests.CQRS.Archetypes;

public class GetArchetypesQueryHandlerTests : CqrsIntegrationTestBase
{
    protected override async Task SeedTestDataAsync()
    {
        var archetypes = new[]
        {
            new ArchetypeDefinition
            {
                Id = "arch-1",
                Name = "The Hero",
                Description = "The archetypal hero"
            },
            new ArchetypeDefinition
            {
                Id = "arch-2",
                Name = "The Trickster",
                Description = "The trickster archetype"
            },
            new ArchetypeDefinition
            {
                Id = "arch-3",
                Name = "The Mentor",
                Description = "The wise mentor"
            }
        };

        await DbContext.ArchetypeDefinitions.AddRangeAsync(archetypes);
        await DbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task GetAllArchetypesQuery_ReturnsAllArchetypes()
    {
        await SeedTestDataAsync();

        var result = await Mediator.Send(new GetAllArchetypesQuery());

        result.Should().HaveCount(3);
        result.Should().Contain(arch => arch.Name == "The Hero");
        result.Should().Contain(arch => arch.Name == "The Trickster");
        result.Should().Contain(arch => arch.Name == "The Mentor");
    }

    [Fact]
    public async Task GetAllArchetypesQuery_WithEmptyDatabase_ReturnsEmptyList()
    {
        var result = await Mediator.Send(new GetAllArchetypesQuery());

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllArchetypesQuery_CachesResultOnSecondCall()
    {
        await SeedTestDataAsync();

        var firstCall = await Mediator.Send(new GetAllArchetypesQuery());
        var secondCall = await Mediator.Send(new GetAllArchetypesQuery());

        firstCall.Should().Equal(secondCall);
    }

    [Fact]
    public async Task GetArchetypeByIdQuery_WithValidId_ReturnsArchetype()
    {
        await SeedTestDataAsync();

        var result = await Mediator.Send(new GetArchetypeByIdQuery("arch-1"));

        result.Should().NotBeNull();
        result!.Name.Should().Be("The Hero");
        result.Description.Should().Be("The archetypal hero");
    }

    [Fact]
    public async Task GetArchetypeByIdQuery_WithInvalidId_ReturnsNull()
    {
        await SeedTestDataAsync();

        var result = await Mediator.Send(new GetArchetypeByIdQuery("invalid-id"));

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetArchetypeByIdQuery_WithEmptyDatabase_ReturnsNull()
    {
        var result = await Mediator.Send(new GetArchetypeByIdQuery("arch-1"));

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllArchetypesQuery_PreservesOrderAndProperties()
    {
        await SeedTestDataAsync();

        var result = await Mediator.Send(new GetAllArchetypesQuery());

        var firstArchetype = result.First();
        firstArchetype.Should().NotBeNull();
        firstArchetype.CreatedAt.Should().NotBe(default);
        firstArchetype.UpdatedAt.Should().NotBe(default);
    }
}
