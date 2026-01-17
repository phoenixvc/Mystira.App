using FluentAssertions;
using Mystira.App.Infrastructure.Discord.Configuration;

namespace Mystira.App.Infrastructure.Discord.Tests;

public class DiscordOptionsTests
{
    [Fact]
    public void DiscordOptions_ShouldHaveCorrectDefaults()
    {
        // Arrange & Act
        var options = new DiscordOptions();

        // Assert
        options.BotToken.Should().BeEmpty();
        options.EnableMessageContentIntent.Should().BeTrue();
        options.EnableGuildMembersIntent.Should().BeFalse();
        options.DefaultTimeoutSeconds.Should().Be(30);
        options.MaxRetryAttempts.Should().Be(3);
        options.LogAllMessages.Should().BeFalse();
        options.CommandPrefix.Should().Be("!");
    }

    [Fact]
    public void DiscordOptions_SectionName_ShouldBeDiscord()
    {
        // Act & Assert
        DiscordOptions.SectionName.Should().Be("Discord");
    }
}
