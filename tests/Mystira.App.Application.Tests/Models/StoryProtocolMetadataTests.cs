using FluentAssertions;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.Tests.Models;

public class StoryProtocolMetadataTests
{
    [Fact]
    public void ValidateContributorSplits_ReturnsTrue_WhenPercentagesSumTo100()
    {
        // Arrange
        var metadata = new StoryProtocolMetadata
        {
            Contributors = new List<Contributor>
            {
                new()
                {
                    Name = "Writer",
                    WalletAddress = "0x1234567890123456789012345678901234567890",
                    Role = ContributorRole.Writer,
                    ContributionPercentage = 50.0m
                },
                new()
                {
                    Name = "Artist",
                    WalletAddress = "0xabcdefabcdefabcdefabcdefabcdefabcdefabcd",
                    Role = ContributorRole.Artist,
                    ContributionPercentage = 30.0m
                },
                new()
                {
                    Name = "Voice Actor",
                    WalletAddress = "0x9876543210987654321098765432109876543210",
                    Role = ContributorRole.VoiceActor,
                    ContributionPercentage = 20.0m
                }
            }
        };

        // Act
        var isValid = metadata.ValidateContributorSplits(out var errors);

        // Assert
        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateContributorSplits_ReturnsFalse_WhenNoContributors()
    {
        // Arrange
        var metadata = new StoryProtocolMetadata
        {
            Contributors = new List<Contributor>()
        };

        // Act
        var isValid = metadata.ValidateContributorSplits(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain("At least one contributor is required.");
    }

    [Fact]
    public void ValidateContributorSplits_ReturnsFalse_WhenPercentagesDontSumTo100()
    {
        // Arrange
        var metadata = new StoryProtocolMetadata
        {
            Contributors = new List<Contributor>
            {
                new()
                {
                    Name = "Writer",
                    WalletAddress = "0x1234567890123456789012345678901234567890",
                    Role = ContributorRole.Writer,
                    ContributionPercentage = 50.0m
                },
                new()
                {
                    Name = "Artist",
                    WalletAddress = "0xabcdefabcdefabcdefabcdefabcdefabcdefabcd",
                    Role = ContributorRole.Artist,
                    ContributionPercentage = 30.0m
                }
            }
        };

        // Act
        var isValid = metadata.ValidateContributorSplits(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("must sum to 100%"));
    }

    [Fact]
    public void ValidateContributorSplits_ReturnsFalse_WhenDuplicateWalletAddresses()
    {
        // Arrange
        var metadata = new StoryProtocolMetadata
        {
            Contributors = new List<Contributor>
            {
                new()
                {
                    Name = "Writer",
                    WalletAddress = "0x1234567890123456789012345678901234567890",
                    Role = ContributorRole.Writer,
                    ContributionPercentage = 50.0m
                },
                new()
                {
                    Name = "Artist",
                    WalletAddress = "0x1234567890123456789012345678901234567890", // Same wallet
                    Role = ContributorRole.Artist,
                    ContributionPercentage = 50.0m
                }
            }
        };

        // Act
        var isValid = metadata.ValidateContributorSplits(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("Duplicate wallet addresses"));
    }

    [Fact]
    public void ValidateContributorSplits_ReturnsFalse_WhenIndividualContributorIsInvalid()
    {
        // Arrange
        var metadata = new StoryProtocolMetadata
        {
            Contributors = new List<Contributor>
            {
                new()
                {
                    Name = "", // Invalid: empty name
                    WalletAddress = "0x1234567890123456789012345678901234567890",
                    Role = ContributorRole.Writer,
                    ContributionPercentage = 100.0m
                }
            }
        };

        // Act
        var isValid = metadata.ValidateContributorSplits(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain("Contributor name cannot be empty.");
    }

    [Fact]
    public void IsRegistered_ReturnsTrue_WhenIpAssetIdIsSet()
    {
        // Arrange
        var metadata = new StoryProtocolMetadata
        {
            IpAssetId = "ip-asset-123"
        };

        // Act & Assert
        metadata.IsRegistered.Should().BeTrue();
    }

    [Fact]
    public void IsRegistered_ReturnsFalse_WhenIpAssetIdIsNull()
    {
        // Arrange
        var metadata = new StoryProtocolMetadata
        {
            IpAssetId = null
        };

        // Act & Assert
        metadata.IsRegistered.Should().BeFalse();
    }

    [Fact]
    public void ContributorCount_ReturnsCorrectCount()
    {
        // Arrange
        var metadata = new StoryProtocolMetadata
        {
            Contributors = new List<Contributor>
            {
                new()
                {
                    Name = "Writer",
                    WalletAddress = "0x1234567890123456789012345678901234567890",
                    Role = ContributorRole.Writer,
                    ContributionPercentage = 50.0m
                },
                new()
                {
                    Name = "Artist",
                    WalletAddress = "0xabcdefabcdefabcdefabcdefabcdefabcdefabcd",
                    Role = ContributorRole.Artist,
                    ContributionPercentage = 50.0m
                }
            }
        };

        // Act & Assert
        metadata.ContributorCount.Should().Be(2);
    }
}
