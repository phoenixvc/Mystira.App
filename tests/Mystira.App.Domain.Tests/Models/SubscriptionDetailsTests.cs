using Mystira.App.Domain.Models;

namespace Mystira.App.Domain.Tests.Models;

public class SubscriptionDetailsTests
{
    #region IsSubscriptionActive Tests

    [Fact]
    public void IsSubscriptionActive_WhenIsActiveIsFalse_ReturnsFalse()
    {
        // Arrange
        var subscription = new SubscriptionDetails
        {
            IsActive = false,
            ValidUntil = DateTime.UtcNow.AddYears(1) // Even with future validity
        };

        // Act
        var result = subscription.IsSubscriptionActive();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsSubscriptionActive_WhenValidUntilIsExpired_ReturnsFalse()
    {
        // Arrange
        var subscription = new SubscriptionDetails
        {
            IsActive = true,
            ValidUntil = DateTime.UtcNow.AddDays(-1) // Expired yesterday
        };

        // Act
        var result = subscription.IsSubscriptionActive();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsSubscriptionActive_WhenActiveAndValidUntilInFuture_ReturnsTrue()
    {
        // Arrange
        var subscription = new SubscriptionDetails
        {
            IsActive = true,
            ValidUntil = DateTime.UtcNow.AddMonths(1) // Valid for another month
        };

        // Act
        var result = subscription.IsSubscriptionActive();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsSubscriptionActive_WhenActiveAndValidUntilIsNull_ReturnsTrue()
    {
        // Arrange - null ValidUntil typically means lifetime subscription
        var subscription = new SubscriptionDetails
        {
            IsActive = true,
            ValidUntil = null
        };

        // Act
        var result = subscription.IsSubscriptionActive();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsSubscriptionActive_WhenValidUntilIsExactlyNow_ReturnsFalse()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var subscription = new SubscriptionDetails
        {
            IsActive = true,
            ValidUntil = now.AddMilliseconds(-1) // Just expired
        };

        // Act
        var result = subscription.IsSubscriptionActive();

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Default Values Tests

    [Fact]
    public void NewSubscription_HasCorrectDefaults()
    {
        // Act
        var subscription = new SubscriptionDetails();

        // Assert
        subscription.Id.Should().NotBeNullOrEmpty();
        subscription.Type.Should().Be(SubscriptionType.Free);
        subscription.IsActive.Should().BeTrue();
        subscription.Tier.Should().Be("Free");
        subscription.PurchasedScenarios.Should().BeEmpty();
    }

    #endregion
}

public class AccountTests
{
    [Fact]
    public void NewAccount_HasCorrectDefaults()
    {
        // Act
        var account = new Account();

        // Assert
        account.Id.Should().NotBeNullOrEmpty();
        account.Role.Should().Be("Guest");
        account.UserProfileIds.Should().BeEmpty();
        account.CompletedScenarioIds.Should().BeEmpty();
        account.Subscription.Should().NotBeNull();
        account.Settings.Should().NotBeNull();
    }

    [Fact]
    public void NewAccount_HasCreatedAtSet()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var account = new Account();

        // Assert
        account.CreatedAt.Should().BeOnOrAfter(before);
        account.LastLoginAt.Should().BeOnOrAfter(before);
    }
}

public class AccountSettingsTests
{
    [Fact]
    public void NewAccountSettings_HasCorrectDefaults()
    {
        // Act
        var settings = new AccountSettings();

        // Assert
        settings.Id.Should().NotBeNullOrEmpty();
        settings.CacheCredentials.Should().BeTrue();
        settings.RequireAuthOnStartup.Should().BeFalse();
        settings.PreferredLanguage.Should().Be("en");
        settings.NotificationsEnabled.Should().BeTrue();
        settings.Theme.Should().Be("Light");
    }
}
