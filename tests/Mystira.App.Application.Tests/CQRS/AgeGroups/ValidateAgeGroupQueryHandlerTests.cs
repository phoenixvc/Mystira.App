using FluentAssertions;
using Mystira.App.Application.CQRS.AgeGroups.Queries;
using Mystira.App.Domain.Models;
using Xunit;

namespace Mystira.App.Application.Tests.CQRS.AgeGroups;

public class ValidateAgeGroupQueryHandlerTests : CqrsIntegrationTestBase
{
    protected override async Task SeedTestDataAsync()
    {
        var ageGroups = new[]
        {
            new AgeGroupDefinition
            {
                Id = "ag-1",
                Name = "Children",
                Value = "5-8",
                MinimumAge = 5,
                MaximumAge = 8,
                Description = "For children"
            },
            new AgeGroupDefinition
            {
                Id = "ag-2",
                Name = "Teens",
                Value = "13-18",
                MinimumAge = 13,
                MaximumAge = 18,
                Description = "For teenagers"
            }
        };

        await DbContext.AgeGroupDefinitions.AddRangeAsync(ageGroups);
        await DbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task Handle_WithExistingValue_ReturnsTrue()
    {
        await SeedTestDataAsync();

        var result = await Mediator.Send(new ValidateAgeGroupQuery("5-8"));

        result.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithNonExistentValue_ReturnsFalse()
    {
        await SeedTestDataAsync();

        var result = await Mediator.Send(new ValidateAgeGroupQuery("invalid"));

        result.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithEmptyDatabase_ReturnsFalse()
    {
        var result = await Mediator.Send(new ValidateAgeGroupQuery("5-8"));

        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("5-8")]
    [InlineData("13-18")]
    public async Task Handle_WithMultipleValidValues_ReturnsTrue(string value)
    {
        await SeedTestDataAsync();

        var result = await Mediator.Send(new ValidateAgeGroupQuery(value));

        result.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_IsCaseInsensitive()
    {
        await SeedTestDataAsync();

        var result = await Mediator.Send(new ValidateAgeGroupQuery("5-8".ToUpper()));

        result.Should().BeTrue();
    }
}
