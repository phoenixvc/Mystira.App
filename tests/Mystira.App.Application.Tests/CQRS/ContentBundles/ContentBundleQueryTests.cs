using FluentAssertions;
using Mystira.App.Application.CQRS.ContentBundles.Queries;
using Mystira.App.Domain.Models;
using Xunit;

namespace Mystira.App.Application.Tests.CQRS.ContentBundles;

public class ContentBundleQueryTests : CqrsIntegrationTestBase
{
    protected override async Task SeedTestDataAsync()
    {
        var bundles = new List<ContentBundle>
        {
            new()
            {
                Id = "bundle-1",
                Title = "Adventure Pack",
                Description = "Collection of adventure stories",
                AgeGroup = "6-9",
                IsFree = false,
                ScenarioIds = new List<string> { "scenario-1", "scenario-2" },
                Prices = new List<BundlePrice>
                {
                    new() { Value = 9.99m, Currency = "USD" }
                }
            },
            new()
            {
                Id = "bundle-2",
                Title = "Free Starter Pack",
                Description = "Free stories for new players",
                AgeGroup = "6-9",
                IsFree = true,
                ScenarioIds = new List<string> { "scenario-3" },
                Prices = new List<BundlePrice>()
            },
            new()
            {
                Id = "bundle-3",
                Title = "Teen Adventures",
                Description = "Stories for teenagers",
                AgeGroup = "13-18",
                IsFree = false,
                ScenarioIds = new List<string> { "scenario-4", "scenario-5" },
                Prices = new List<BundlePrice>
                {
                    new() { Value = 14.99m, Currency = "USD" },
                    new() { Value = 12.99m, Currency = "EUR" }
                }
            }
        };

        DbContext.ContentBundles.AddRange(bundles);
        await DbContext.SaveChangesAsync();
    }

    #region GetAllContentBundlesQuery Tests

    [Fact]
    public async Task GetAllContentBundlesQuery_ReturnsAllBundles()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new GetAllContentBundlesQuery();

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetAllContentBundlesQuery_WithNoBundles_ReturnsEmptyList()
    {
        // Arrange - no seeding
        var query = new GetAllContentBundlesQuery();

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllContentBundlesQuery_ReturnsCorrectData()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new GetAllContentBundlesQuery();

        // Act
        var result = (await Mediator.Send(query)).ToList();

        // Assert
        var adventurePack = result.FirstOrDefault(b => b.Id == "bundle-1");
        adventurePack.Should().NotBeNull();
        adventurePack!.Title.Should().Be("Adventure Pack");
        adventurePack.ScenarioIds.Should().HaveCount(2);
        adventurePack.Prices.Should().HaveCount(1);
        adventurePack.Prices[0].Value.Should().Be(9.99m);
    }

    #endregion

    #region GetContentBundlesByAgeGroupQuery Tests

    [Fact]
    public async Task GetContentBundlesByAgeGroupQuery_ReturnsMatchingBundles()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new GetContentBundlesByAgeGroupQuery("6-9");

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().OnlyContain(b => b.AgeGroup == "6-9");
    }

    [Fact]
    public async Task GetContentBundlesByAgeGroupQuery_WithNoMatches_ReturnsEmptyList()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new GetContentBundlesByAgeGroupQuery("1-2");

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetContentBundlesByAgeGroupQuery_TeenAgeGroup_ReturnsTeenBundles()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new GetContentBundlesByAgeGroupQuery("13-18");

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().Title.Should().Be("Teen Adventures");
    }

    #endregion

    #region ContentBundle Model Tests

    [Fact]
    public void ContentBundle_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var bundle = new ContentBundle();

        // Assert
        bundle.Id.Should().NotBeNullOrEmpty();
        bundle.Title.Should().BeEmpty();
        bundle.Description.Should().BeEmpty();
        bundle.ScenarioIds.Should().NotBeNull().And.BeEmpty();
        bundle.Prices.Should().NotBeNull().And.BeEmpty();
        bundle.IsFree.Should().BeFalse();
        bundle.AgeGroup.Should().BeEmpty();
    }

    [Fact]
    public void BundlePrice_DefaultCurrency_IsUSD()
    {
        // Arrange & Act
        var price = new BundlePrice { Value = 10.00m };

        // Assert
        price.Currency.Should().Be("USD");
    }

    #endregion
}
