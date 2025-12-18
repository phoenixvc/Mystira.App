using FluentAssertions;
using Mystira.App.Application.CQRS.Archetypes.Queries;
using Mystira.App.Domain.Models;
using Xunit;

namespace Mystira.App.Application.Tests.CQRS.Archetypes;

public class ValidateArchetypeQueryHandlerTests : CqrsIntegrationTestBase
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
            }
        };

        await DbContext.ArchetypeDefinitions.AddRangeAsync(archetypes);
        await DbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task Handle_WithExistingName_ReturnsTrue()
    {
        await SeedTestDataAsync();

        var result = await Mediator.Send(new ValidateArchetypeQuery("The Hero"));

        result.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithNonExistentName_ReturnsFalse()
    {
        await SeedTestDataAsync();

        var result = await Mediator.Send(new ValidateArchetypeQuery("Nonexistent"));

        result.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithEmptyDatabase_ReturnsFalse()
    {
        var result = await Mediator.Send(new ValidateArchetypeQuery("The Hero"));

        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("The Hero")]
    [InlineData("The Trickster")]
    public async Task Handle_WithMultipleValidNames_ReturnsTrue(string name)
    {
        await SeedTestDataAsync();

        var result = await Mediator.Send(new ValidateArchetypeQuery(name));

        result.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_IsCaseInsensitive()
    {
        await SeedTestDataAsync();

        var result = await Mediator.Send(new ValidateArchetypeQuery("the hero"));

        result.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithWhitespaceAroundName_ReturnsFalse()
    {
        await SeedTestDataAsync();

        var result = await Mediator.Send(new ValidateArchetypeQuery(" The Hero "));

        result.Should().BeFalse();
    }
}
