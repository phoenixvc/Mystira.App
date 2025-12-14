using System.Threading.Tasks;
using Mystira.App.Domain.Models;
using FluentAssertions;
using Xunit;

namespace Mystira.App.Application.Tests.Models;

public class RandomNameGeneratorTests
{
    [Fact]
    public void GenerateFantasyName_ReturnsNameFromFantasyList()
    {
        // Act
        var name = RandomNameGenerator.GenerateFantasyName();

        // Assert
        name.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateAdjectiveName_ReturnsAdjectiveAndName()
    {
        // Act
        var name = RandomNameGenerator.GenerateAdjectiveName();

        // Assert
        name.Should().Contain(" ");
    }

    [Fact]
    public void GenerateGuestName_CanReturnBothTypes()
    {
        // Act
        var fantasyName = RandomNameGenerator.GenerateGuestName(false);
        var adjectiveName = RandomNameGenerator.GenerateGuestName(true);

        // Assert
        fantasyName.Should().NotBeNullOrEmpty();
        adjectiveName.Should().Contain(" ");
    }

    [Fact]
    public void GenerateUniqueGuestNames_ReturnsCorrectCount()
    {
        // Arrange
        const int count = 5;

        // Act
        var names = RandomNameGenerator.GenerateUniqueGuestNames(count);

        // Assert
        names.Should().HaveCount(count);
    }

    [Fact]
    public void GenerateUniqueGuestNames_ReturnsUniqueNames()
    {
        // Arrange
        const int count = 10;

        // Act
        var names = RandomNameGenerator.GenerateUniqueGuestNames(count, useAdjective: true);

        // Assert
        names.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void GenerateUniqueGuestNames_ThreadSafetyTest()
    {
        // Arrange
        const int numThreads = 10;
        const int namesPerThread = 100;
        var tasks = new Task<List<string>>[numThreads];

        // Act
        for (var i = 0; i < numThreads; i++)
        {
            tasks[i] = Task.Run(() => RandomNameGenerator.GenerateUniqueGuestNames(namesPerThread, true));
        }

        Task.WhenAll(tasks).Wait();

        // Assert
        var allNames = new List<string>();
        foreach (var task in tasks)
        {
            allNames.AddRange(task.Result);
        }

        allNames.Should().HaveCount(numThreads * namesPerThread);
    }

    [Fact]
    public void GenerateUniqueGuestNames_ThrowsExceptionWhenRequestingTooManyNames()
    {
        // Arrange
        const int count = 10000;

        // Act
        Action act = () => RandomNameGenerator.GenerateUniqueGuestNames(count, useAdjective: true);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

}
