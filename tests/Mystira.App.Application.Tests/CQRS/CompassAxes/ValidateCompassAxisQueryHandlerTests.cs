using FluentAssertions;
using Mystira.App.Application.CQRS.CompassAxes.Queries;
using Mystira.App.Domain.Models;
using Xunit;

namespace Mystira.App.Application.Tests.CQRS.CompassAxes;

public class ValidateCompassAxisQueryHandlerTests : CqrsIntegrationTestBase
{
    protected override async Task SeedTestDataAsync()
    {
        var axes = new[]
        {
            new CompassAxis
            {
                Id = "axis-1",
                Name = "Chaos-Order",
                Description = "From chaos to order"
            },
            new CompassAxis
            {
                Id = "axis-2",
                Name = "Good-Evil",
                Description = "From good to evil"
            }
        };

        await DbContext.CompassAxes.AddRangeAsync(axes);
        await DbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task Handle_WithExistingName_ReturnsTrue()
    {
        await SeedTestDataAsync();

        var result = await Mediator.Send(new ValidateCompassAxisQuery("Chaos-Order"));

        result.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithNonExistentName_ReturnsFalse()
    {
        await SeedTestDataAsync();

        var result = await Mediator.Send(new ValidateCompassAxisQuery("Nonexistent"));

        result.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithEmptyDatabase_ReturnsFalse()
    {
        var result = await Mediator.Send(new ValidateCompassAxisQuery("Chaos-Order"));

        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("Chaos-Order")]
    [InlineData("Good-Evil")]
    public async Task Handle_WithMultipleValidNames_ReturnsTrue(string name)
    {
        await SeedTestDataAsync();

        var result = await Mediator.Send(new ValidateCompassAxisQuery(name));

        result.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_IsCaseInsensitive()
    {
        await SeedTestDataAsync();

        var result = await Mediator.Send(new ValidateCompassAxisQuery("chaos-order"));

        result.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithWhitespaceAroundName_ReturnsFalse()
    {
        await SeedTestDataAsync();

        var result = await Mediator.Send(new ValidateCompassAxisQuery(" Chaos-Order "));

        result.Should().BeFalse();
    }
}
