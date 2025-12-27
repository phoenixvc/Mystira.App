using FluentAssertions;
using Mystira.Shared.Extensions;

namespace Mystira.App.Application.Tests.Helpers;

/// <summary>
/// Tests for Mystira.Shared.Extensions string extension methods.
/// These tests validate the actual behavior of the shared library.
/// </summary>
public class StringExtensionsTests
{
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("hello", "Hello")]
    [InlineData("HELLO", "Hello")]
    [InlineData("hello world", "Hello World")]
    [InlineData("hello_world", "Hello_World")] // Preserves underscores
    [InlineData("hello-world", "Hello-World")] // Preserves hyphens
    public void ToTitleCase_ConvertsCorrectly(string? input, string expected)
    {
        // Act
        var result = input.ToTitleCase();

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(null, 10, "")]
    [InlineData("", 10, "")]
    [InlineData("short", 10, "short")]
    [InlineData("exactly10!", 10, "exactly10!")]
    [InlineData("this is a longer string", 10, "this is a ")]
    [InlineData("this is a longer string", 15, "this is a longe")]
    public void Truncate_TruncatesCorrectly(string? input, int maxLength, string expected)
    {
        // Act
        var result = input.Truncate(maxLength);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void Truncate_WithLongString_TruncatesToMaxLength()
    {
        // Arrange
        var input = "this is a long string that needs truncation";

        // Act
        var result = input.Truncate(20);

        // Assert
        result.Length.Should().BeLessThanOrEqualTo(20);
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("hello", "hello")]
    [InlineData("Hello", "hello")]
    [InlineData("HelloWorld", "hello_world")]
    [InlineData("helloWorld", "hello_world")]
    [InlineData("userId", "user_id")]
    [InlineData("ID", "id")]  // Consecutive caps treated as word
    [InlineData("UserID", "user_id")]
    public void ToSnakeCase_ConvertsCorrectly(string? input, string expected)
    {
        // Act
        var result = input.ToSnakeCase();

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void ToTitleCase_WithMixedSeparators_PreservesSeparators()
    {
        // Arrange
        var input = "mixed_separators-and spaces";

        // Act
        var result = input.ToTitleCase();

        // Assert - The shared extension preserves separators
        result.Should().Be("Mixed_Separators-And Spaces");
    }

    [Fact]
    public void ToTitleCase_WithSingleCharacterWords_HandlesCorrectly()
    {
        // Arrange
        var input = "a_b_c";

        // Act
        var result = input.ToTitleCase();

        // Assert - Underscores are preserved
        result.Should().Be("A_B_C");
    }

    [Fact]
    public void Truncate_WithVeryShortMaxLength_HandlesEdgeCase()
    {
        // Arrange
        var input = "hello";

        // Act
        var result = input.Truncate(2);

        // Assert
        result.Length.Should().Be(2);
        result.Should().Be("he");
    }

    [Fact]
    public void ToTitleCase_WithSpaceSeparatedWords_TitleCasesEach()
    {
        // Arrange
        var input = "hello world test";

        // Act
        var result = input.ToTitleCase();

        // Assert
        result.Should().Be("Hello World Test");
    }

    [Theory]
    [InlineData("PascalCase", "pascal_case")]
    [InlineData("camelCase", "camel_case")]
    [InlineData("alreadylowercase", "alreadylowercase")]
    public void ToSnakeCase_WithVariousCasings_ConvertsCorrectly(string input, string expected)
    {
        // Act
        var result = input.ToSnakeCase();

        // Assert
        result.Should().Be(expected);
    }
}
