using System.Reflection;
using FluentAssertions;
using Mystira.App.Infrastructure.Discord.Modules;

namespace Mystira.App.Infrastructure.Discord.Tests;

/// <summary>
/// Unit tests for TicketModule channel name generation logic.
/// Tests the MakeSafeChannelSlug method and channel name length constraints.
/// </summary>
public class TicketModuleTests
{
    // Access the private static MakeSafeChannelSlug method via reflection
    private static readonly MethodInfo MakeSafeChannelSlugMethod =
        typeof(TicketModule).GetMethod("MakeSafeChannelSlug",
            BindingFlags.NonPublic | BindingFlags.Static)!;

    private static string MakeSafeChannelSlug(string input)
    {
        return (string)MakeSafeChannelSlugMethod.Invoke(null, new object[] { input })!;
    }

    [Theory]
    [InlineData("JohnDoe", "johndoe")]
    [InlineData("john_doe", "john-doe")]
    [InlineData("John Doe", "john-doe")]
    [InlineData("UPPERCASE", "uppercase")]
    [InlineData("123numbers", "123numbers")]
    public void MakeSafeChannelSlug_ShouldNormalizeUsernames(string input, string expected)
    {
        // Act
        var result = MakeSafeChannelSlug(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("---test---", "test")]
    [InlineData("--leading", "leading")]
    [InlineData("trailing--", "trailing")]
    public void MakeSafeChannelSlug_ShouldTrimDashes(string input, string expected)
    {
        // Act
        var result = MakeSafeChannelSlug(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("a  b", "a-b")]
    [InlineData("a___b", "a-b")]
    [InlineData("test!!!name", "test-name")]
    public void MakeSafeChannelSlug_ShouldCollapseConsecutiveSpecialChars(string input, string expected)
    {
        // Act
        var result = MakeSafeChannelSlug(input);

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
        var result = MakeSafeChannelSlug(input);

        // Assert
        result.Should().Be("user");
    }

    [Theory]
    [InlineData("日本語", "user")] // Non-ASCII gets stripped, falls back to "user"
    [InlineData("émoji👍", "moji")] // 'e' with accent and emoji stripped
    public void MakeSafeChannelSlug_WithNonAsciiCharacters_ShouldHandleGracefully(string input, string expected)
    {
        // Act
        var result = MakeSafeChannelSlug(input);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void ChannelNameLength_ShouldNotExceed100Characters()
    {
        // Arrange - Discord channel name limit is 100 characters
        // Format: "ticket-" (7) + safeName + "-" (1) + suffix (4) = 12 + safeName
        // Max safeName length: 100 - 12 = 88 chars
        const int maxChannelLength = 100;
        const int overheadLength = 12; // "ticket-" + "-" + "1234"
        const int maxSafeNameLength = maxChannelLength - overheadLength; // 88

        // Create a username that's longer than the maximum
        var longUsername = new string('a', 100);
        var safeName = MakeSafeChannelSlug(longUsername);

        // Simulate the truncation logic from TicketModule
        if (safeName.Length > maxSafeNameLength)
        {
            safeName = safeName[..maxSafeNameLength];
        }

        var channelName = $"ticket-{safeName}-1234";

        // Assert
        channelName.Length.Should().BeLessThanOrEqualTo(maxChannelLength);
        safeName.Length.Should().Be(maxSafeNameLength);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(50, 50)]
    [InlineData(88, 88)]
    [InlineData(89, 88)] // Should be truncated
    [InlineData(100, 88)] // Should be truncated
    [InlineData(200, 88)] // Should be truncated
    public void ChannelNameLength_WithVariousUsernameLengths_ShouldRespectLimit(int usernameLength, int expectedSafeNameLength)
    {
        // Arrange
        const int maxSafeNameLength = 88;
        var username = new string('x', usernameLength);
        var safeName = MakeSafeChannelSlug(username);

        // Simulate truncation
        if (safeName.Length > maxSafeNameLength)
        {
            safeName = safeName[..maxSafeNameLength];
        }

        // Assert
        safeName.Length.Should().Be(expectedSafeNameLength);

        // Full channel name should never exceed 100
        var channelName = $"ticket-{safeName}-9999";
        channelName.Length.Should().BeLessThanOrEqualTo(100);
    }

    [Fact]
    public void ChannelNameFormat_ShouldFollowExpectedPattern()
    {
        // Arrange
        var username = "TestUser123";
        var safeName = MakeSafeChannelSlug(username);
        var suffix = 5678;

        // Act
        var channelName = $"ticket-{safeName}-{suffix}";

        // Assert
        channelName.Should().Be("ticket-testuser123-5678");
        channelName.Should().MatchRegex(@"^ticket-[a-z0-9-]+-\d{4}$");
    }
}
