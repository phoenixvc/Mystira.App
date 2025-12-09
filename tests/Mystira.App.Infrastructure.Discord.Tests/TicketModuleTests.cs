using FluentAssertions;
using Mystira.App.Infrastructure.Discord.Utilities;

namespace Mystira.App.Infrastructure.Discord.Tests;

public class TicketModuleTests
{
    [Theory]
    [InlineData("JohnDoe", "johndoe")]
    [InlineData("UPPERCASE", "uppercase")]
    [InlineData("john_doe", "john-doe")]
    [InlineData("John Doe", "john-doe")]
    [InlineData("123numbers", "123numbers")]
    public void MakeSafeChannelSlug_ShouldNormalizeUsernames(string input, string expected)
    {
        // Act
        var result = ChannelNameSanitizer.MakeSafeChannelSlug(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("---")]
    [InlineData("!!!")]
    public void MakeSafeChannelSlug_WithEmptyOrSpecialOnlyInput_ShouldReturnUser(string input)
    {
        // Act
        var result = ChannelNameSanitizer.MakeSafeChannelSlug(input);

        // Assert
        result.Should().Be("user");
    }

    [Fact]
    public void ChannelNameLength_ShouldNotExceed100Characters()
    {
        // Arrange
        var longUsername = new string('a', 200);

        // Act
        var safeName = ChannelNameSanitizer.MakeSafeChannelSlug(longUsername);
        var channelName = $"ticket-{safeName}-1234";

        // Assert
        channelName.Length.Should().BeLessThanOrEqualTo(100);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(50, 50)]
    [InlineData(88, 88)]
    [InlineData(89, 88)]
    [InlineData(100, 88)]
    [InlineData(200, 88)]
    public void ChannelNameLength_WithVariousUsernameLengths_ShouldRespectLimit(int usernameLength, int expectedSafeNameLength)
    {
        // Arrange
        var username = new string('a', usernameLength);

        // Act
        var safeName = ChannelNameSanitizer.MakeSafeChannelSlug(username);

        // Assert
        safeName.Length.Should().Be(expectedSafeNameLength);
    }

    [Theory]
    [InlineData("a___b", "a-b")]
    [InlineData("test!!!name", "test-name")]
    [InlineData("a  b", "a-b")]
    public void MakeSafeChannelSlug_ShouldCollapseConsecutiveSpecialChars(string input, string expected)
    {
        // Act
        var result = ChannelNameSanitizer.MakeSafeChannelSlug(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("--leading", "leading")]
    [InlineData("trailing--", "trailing")]
    [InlineData("---test---", "test")]
    public void MakeSafeChannelSlug_ShouldTrimDashes(string input, string expected)
    {
        // Act
        var result = ChannelNameSanitizer.MakeSafeChannelSlug(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("日本語", "user")]
    [InlineData("émoji👍", "moji")]
    public void MakeSafeChannelSlug_WithNonAsciiCharacters_ShouldHandleGracefully(string input, string expected)
    {
        // Act
        var result = ChannelNameSanitizer.MakeSafeChannelSlug(input);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void ChannelNameFormat_ShouldFollowExpectedPattern()
    {
        // Arrange
        var username = "TestUser123";

        // Act
        var safeName = ChannelNameSanitizer.MakeSafeChannelSlug(username);
        var channelName = $"ticket-{safeName}-1234";

        // Assert
        channelName.Should().MatchRegex(@"^ticket-[a-z0-9\-]+-\d{4}$");
    }
}
