using FluentAssertions;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.Tests.Models;

public class ContributorTests
{
    [Fact]
    public void Validate_ReturnsTrue_WhenContributorIsValid()
    {
        // Arrange
        var contributor = new Contributor
        {
            Name = "John Smith",
            WalletAddress = "0x1234567890123456789012345678901234567890",
            Role = ContributorRole.Writer,
            ContributionPercentage = 50.0m
        };

        // Act
        var isValid = contributor.Validate(out var errors);

        // Assert
        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_ReturnsFalse_WhenNameIsEmpty()
    {
        // Arrange
        var contributor = new Contributor
        {
            Name = "",
            WalletAddress = "0x1234567890123456789012345678901234567890",
            Role = ContributorRole.Writer,
            ContributionPercentage = 50.0m
        };

        // Act
        var isValid = contributor.Validate(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain("Contributor name cannot be empty.");
    }

    [Fact]
    public void Validate_ReturnsFalse_WhenWalletAddressIsEmpty()
    {
        // Arrange
        var contributor = new Contributor
        {
            Name = "John Smith",
            WalletAddress = "",
            Role = ContributorRole.Writer,
            ContributionPercentage = 50.0m
        };

        // Act
        var isValid = contributor.Validate(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain("Wallet address cannot be empty.");
    }

    [Fact]
    public void Validate_ReturnsFalse_WhenWalletAddressFormatIsInvalid()
    {
        // Arrange
        var contributor = new Contributor
        {
            Name = "John Smith",
            WalletAddress = "invalid-address",
            Role = ContributorRole.Writer,
            ContributionPercentage = 50.0m
        };

        // Act
        var isValid = contributor.Validate(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain("Wallet address format is invalid.");
    }

    [Fact]
    public void Validate_ReturnsFalse_WhenContributionPercentageIsNegative()
    {
        // Arrange
        var contributor = new Contributor
        {
            Name = "John Smith",
            WalletAddress = "0x1234567890123456789012345678901234567890",
            Role = ContributorRole.Writer,
            ContributionPercentage = -10.0m
        };

        // Act
        var isValid = contributor.Validate(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain("Contribution percentage must be between 0 and 100.");
    }

    [Fact]
    public void Validate_ReturnsFalse_WhenContributionPercentageExceeds100()
    {
        // Arrange
        var contributor = new Contributor
        {
            Name = "John Smith",
            WalletAddress = "0x1234567890123456789012345678901234567890",
            Role = ContributorRole.Writer,
            ContributionPercentage = 110.0m
        };

        // Act
        var isValid = contributor.Validate(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain("Contribution percentage must be between 0 and 100.");
    }
}
