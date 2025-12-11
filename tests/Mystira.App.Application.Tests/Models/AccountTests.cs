using Mystira.App.Domain.Models;
using FluentAssertions;
using Xunit;

namespace Mystira.App.Application.Tests.Models;

public class AccountTests
{
    [Fact]
    public void IsSubscriptionActive_ReturnsTrue_WhenSubscriptionIsActiveAndNotExpired()
    {
        // Arrange
        var subscription = new SubscriptionDetails
        {
            IsActive = true,
            ValidUntil = DateTime.UtcNow.AddDays(1)
        };

        // Act
        var isActive = subscription.IsSubscriptionActive();

        // Assert
        isActive.Should().BeTrue();
    }

    [Fact]
    public void IsSubscriptionActive_ReturnsFalse_WhenSubscriptionIsNotActive()
    {
        // Arrange
        var subscription = new SubscriptionDetails
        {
            IsActive = false,
            ValidUntil = DateTime.UtcNow.AddDays(1)
        };

        // Act
        var isActive = subscription.IsSubscriptionActive();

        // Assert
        isActive.Should().BeFalse();
    }

    [Fact]
    public void IsSubscriptionActive_ReturnsFalse_WhenSubscriptionIsExpired()
    {
        // Arrange
        var subscription = new SubscriptionDetails
        {
            IsActive = true,
            ValidUntil = DateTime.UtcNow.AddDays(-1)
        };

        // Act
        var isActive = subscription.IsSubscriptionActive();

        // Assert
        isActive.Should().BeFalse();
    }

    [Fact]
    public void IsSubscriptionActive_ReturnsTrue_WhenSubscriptionIsLifetime()
    {
        // Arrange
        var subscription = new SubscriptionDetails
        {
            IsActive = true,
            ValidUntil = null
        };

        // Act
        var isActive = subscription.IsSubscriptionActive();

        // Assert
        isActive.Should().BeTrue();
    }
}
