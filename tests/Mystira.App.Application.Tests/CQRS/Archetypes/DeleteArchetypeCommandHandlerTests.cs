using FluentAssertions;
using Mystira.App.Application.CQRS.Archetypes.Commands;
using Mystira.App.Application.CQRS.Archetypes.Queries;
using Mystira.App.Domain.Models;
using Xunit;

namespace Mystira.App.Application.Tests.CQRS.Archetypes;

public class DeleteArchetypeCommandHandlerTests : CqrsIntegrationTestBase
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
    public async Task Handle_WithValidId_DeletesArchetype()
    {
        await SeedTestDataAsync();

        var result = await Mediator.Send(new DeleteArchetypeCommand("arch-1"));

        result.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithInvalidId_ReturnsFalse()
    {
        var result = await Mediator.Send(new DeleteArchetypeCommand("invalid-id"));

        result.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_SoftDeletesEntity()
    {
        await SeedTestDataAsync();

        await Mediator.Send(new DeleteArchetypeCommand("arch-1"));

        var deleted = await DbContext.ArchetypeDefinitions.FindAsync("arch-1");
        deleted.Should().NotBeNull();
        deleted.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_DeletesOnlyTargetEntity()
    {
        await SeedTestDataAsync();

        await Mediator.Send(new DeleteArchetypeCommand("arch-1"));

        var remaining = await Mediator.Send(new GetAllArchetypesQuery());
        remaining.Should().HaveCount(1);
        remaining.First().Id.Should().Be("arch-2");
    }

    [Fact]
    public async Task Handle_InvalidatesCachePrefix()
    {
        ClearCache();
        await SeedTestDataAsync();

        // First call to seed the cache
        var initialArchetypes = await Mediator.Send(new GetAllArchetypesQuery());
        initialArchetypes.Should().HaveCount(2);

        // Verify it's actually cached
        var cacheKey = new GetAllArchetypesQuery().CacheKey;
        Cache.TryGetValue(cacheKey, out _).Should().BeTrue();

        await Mediator.Send(new DeleteArchetypeCommand("arch-1"));

        // Verify cache entry is gone
        Cache.TryGetValue(cacheKey, out _).Should().BeFalse();

        var updatedArchetypes = await Mediator.Send(new GetAllArchetypesQuery());
        updatedArchetypes.Should().HaveCount(1);
        updatedArchetypes.First().Id.Should().Be("arch-2");
    }

    [Fact]
    public async Task Handle_WithAlreadyDeletedId_ReturnsFalse()
    {
        await SeedTestDataAsync();
        await Mediator.Send(new DeleteArchetypeCommand("arch-1"));

        var secondResult = await Mediator.Send(new DeleteArchetypeCommand("arch-1"));

        secondResult.Should().BeFalse();
    }
}
