using FluentAssertions;
using Mystira.Shared.Extensions;

namespace Mystira.App.Application.Tests.Helpers;

public class StringExtensionsTests
{
    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("hello", "Hello")]
    [InlineData("HELLO", "Hello")]
    [InlineData("hello world", "Hello World")]
    [InlineData("hello_world", "Hello World")]
    [InlineData("hello-world", "Hello World")]
    [InlineData("hello_WORLD", "Hello World")]
    [InlineData("HELLO_WORLD", "Hello World")]
    [InlineData("hello_world_test", "Hello World Test")]
    [InlineData("hello-world-test", "Hello World Test")]
    [InlineData("hello world_test-example", "Hello World Test Example")]
    public void ToTitleCase_ConvertsCorrectly(string? input, string? expected)
    {
        // Act
        var result = input.ToTitleCase();

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(null, 10, "...", null)]
    [InlineData("", 10, "...", "")]
    [InlineData("short", 10, "...", "short")]
    [InlineData("exactly10!", 10, "...", "exactly10!")]
    [InlineData("this is a longer string", 10, "...", "this is...")]
    [InlineData("this is a longer string", 15, "...", "this is a lo...")]
    [InlineData("truncate me", 8, "~", "truncat~")]
    public void Truncate_TruncatesCorrectly(string? input, int maxLength, string suffix, string? expected)
    {
        // Act
        var result = input.Truncate(maxLength, suffix);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void Truncate_WithDefaultSuffix_UsesEllipsis()
    {
        // Arrange
        var input = "this is a long string that needs truncation";

        // Act
        var result = input.Truncate(20);

        // Assert
        result.Should().EndWith("...");
        result.Length.Should().BeLessThanOrEqualTo(20);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("hello", "hello")]
    [InlineData("Hello", "hello")]
    [InlineData("HelloWorld", "hello_world")]
    [InlineData("helloWorld", "hello_world")]
    [InlineData("HTTPRequest", "h_t_t_p_request")]
    [InlineData("ID", "i_d")]
    [InlineData("userId", "user_id")]
    [InlineData("UserID", "user_i_d")]
    public void ToSnakeCase_ConvertsCorrectly(string? input, string? expected)
    {
        // Act
        var result = input.ToSnakeCase();

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void ToTitleCase_WithMixedSeparators_HandlesAllCorrectly()
    {
        // Arrange
        var input = "mixed_separators-and spaces";

        // Act
        var result = input.ToTitleCase();

        // Assert
        result.Should().Be("Mixed Separators And Spaces");
    }

    [Fact]
    public void ToTitleCase_WithSingleCharacterWords_HandlesCorrectly()
    {
        // Arrange
        var input = "a_b_c";

        // Act
        var result = input.ToTitleCase();

        // Assert
        result.Should().Be("A B C");
    }

    [Fact]
    public void Truncate_WithVeryShortMaxLength_HandlesEdgeCase()
    {
        // Arrange
        var input = "hello";

        // Act - maxLength less than suffix length
        var result = input.Truncate(2, "...");

        // Assert - should return truncated even if it means cutting into suffix
        result.Length.Should().Be(2);
    }
}
