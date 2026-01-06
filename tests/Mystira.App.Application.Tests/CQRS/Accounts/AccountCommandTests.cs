using FluentAssertions;
using Mystira.App.Application.CQRS.Accounts.Commands;
using Mystira.App.Application.CQRS.Accounts.Queries;
using Mystira.App.Domain.Models;
using Xunit;

namespace Mystira.App.Application.Tests.CQRS.Accounts;

public class AccountCommandTests : CqrsIntegrationTestBase
{
    #region CreateAccountCommand Tests

    [Fact]
    public async Task CreateAccountCommand_WithValidData_CreatesAccount()
    {
        // Arrange
        var command = new CreateAccountCommand(
            Auth0UserId: "auth0|123",
            Email: "test@example.com",
            DisplayName: "TestUser",
            UserProfileIds: null,
            Subscription: null,
            Settings: null
        );

        // Act
        var result = await Mediator.Send(command);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeNullOrEmpty();
        result.Auth0UserId.Should().Be("auth0|123");
        result.Email.Should().Be("test@example.com");
        result.DisplayName.Should().Be("TestUser");
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.LastLoginAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Verify persistence
        var savedAccount = await DbContext.Accounts.FindAsync(result.Id);
        savedAccount.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateAccountCommand_WithoutDisplayName_UsesEmailPrefix()
    {
        // Arrange
        var command = new CreateAccountCommand(
            Auth0UserId: "auth0|456",
            Email: "newuser@example.com",
            DisplayName: null,
            UserProfileIds: null,
            Subscription: null,
            Settings: null
        );

        // Act
        var result = await Mediator.Send(command);

        // Assert
        result.Should().NotBeNull();
        result.DisplayName.Should().Be("newuser");
    }

    [Fact]
    public async Task CreateAccountCommand_WithUserProfileIds_LinksProfiles()
    {
        // Arrange
        var profileIds = new List<string> { "profile-1", "profile-2" };
        var command = new CreateAccountCommand(
            Auth0UserId: "auth0|789",
            Email: "linked@example.com",
            DisplayName: "LinkedUser",
            UserProfileIds: profileIds,
            Subscription: null,
            Settings: null
        );

        // Act
        var result = await Mediator.Send(command);

        // Assert
        result.Should().NotBeNull();
        result.UserProfileIds.Should().HaveCount(2);
        result.UserProfileIds.Should().Contain("profile-1");
        result.UserProfileIds.Should().Contain("profile-2");
    }

    [Fact]
    public async Task CreateAccountCommand_WithSubscription_SetsSubscription()
    {
        // Arrange
        var subscription = new SubscriptionDetails
        {
            Tier = "premium",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddYears(1)
        };

        var command = new CreateAccountCommand(
            Auth0UserId: "auth0|sub",
            Email: "premium@example.com",
            DisplayName: "PremiumUser",
            UserProfileIds: null,
            Subscription: subscription,
            Settings: null
        );

        // Act
        var result = await Mediator.Send(command);

        // Assert
        result.Should().NotBeNull();
        result.Subscription.Should().NotBeNull();
        result.Subscription!.Tier.Should().Be("premium");
    }

    [Fact]
    public async Task CreateAccountCommand_WithSettings_SetsSettings()
    {
        // Arrange
        var settings = new AccountSettings
        {
            NotificationsEnabled = true,
            Theme = "dark"
        };
        var command = new CreateAccountCommand(
            Auth0UserId: "auth0|settings",
            Email: "settings@example.com",
            DisplayName: "SettingsUser",
            UserProfileIds: null,
            Subscription: null,
            Settings: settings
        );

        // Act
        var result = await Mediator.Send(command);

        // Assert
        result.Should().NotBeNull();
        result.Settings.Should().NotBeNull();
        result.Settings!.NotificationsEnabled.Should().BeTrue();
        result.Settings.Theme.Should().Be("dark");
    }

    [Fact]
    public async Task CreateAccountCommand_WithDuplicateEmail_ThrowsInvalidOperationException()
    {
        // Arrange - Create existing account
        var existingAccount = new Account
        {
            Id = "existing-account",
            Auth0UserId = "auth0|existing",
            Email = "duplicate@example.com",
            DisplayName = "Existing"
        };
        DbContext.Accounts.Add(existingAccount);
        await DbContext.SaveChangesAsync();

        var command = new CreateAccountCommand(
            Auth0UserId: "auth0|new",
            Email: "duplicate@example.com",
            DisplayName: "NewUser",
            UserProfileIds: null,
            Subscription: null,
            Settings: null
        );

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await Mediator.Send(command);
        });
    }

    #endregion

    #region DeleteAccountCommand Tests

    [Fact]
    public async Task DeleteAccountCommand_WithExistingAccount_DeletesAccount()
    {
        // Arrange
        var account = new Account
        {
            Id = "account-to-delete",
            Auth0UserId = "auth0|delete",
            Email = "todelete@example.com",
            DisplayName = "ToDelete"
        };
        DbContext.Accounts.Add(account);
        await DbContext.SaveChangesAsync();

        var command = new DeleteAccountCommand("account-to-delete");

        // Act
        var result = await Mediator.Send(command);

        // Assert
        result.Should().BeTrue();
        var deletedAccount = await DbContext.Accounts.FindAsync("account-to-delete");
        deletedAccount.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAccountCommand_WithNonExistentAccount_ReturnsFalse()
    {
        // Arrange
        var command = new DeleteAccountCommand("non-existent");

        // Act
        var result = await Mediator.Send(command);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetAccountQuery Tests

    [Fact]
    public async Task GetAccountQuery_WithExistingAccount_ReturnsAccount()
    {
        // Arrange
        var account = new Account
        {
            Id = "account-1",
            Auth0UserId = "auth0|get",
            Email = "get@example.com",
            DisplayName = "GetUser"
        };
        DbContext.Accounts.Add(account);
        await DbContext.SaveChangesAsync();

        var query = new GetAccountQuery("account-1");

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("account-1");
        result.Email.Should().Be("get@example.com");
    }

    [Fact]
    public async Task GetAccountQuery_WithNonExistentAccount_ReturnsNull()
    {
        // Arrange
        var query = new GetAccountQuery("non-existent");

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetAccountByEmailQuery Tests

    [Fact]
    public async Task GetAccountByEmailQuery_WithExistingEmail_ReturnsAccount()
    {
        // Arrange
        var account = new Account
        {
            Id = "account-email",
            Auth0UserId = "auth0|email",
            Email = "find@example.com",
            DisplayName = "FindUser"
        };
        DbContext.Accounts.Add(account);
        await DbContext.SaveChangesAsync();

        var query = new GetAccountByEmailQuery("find@example.com");

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be("find@example.com");
    }

    [Fact]
    public async Task GetAccountByEmailQuery_WithNonExistentEmail_ReturnsNull()
    {
        // Arrange
        var query = new GetAccountByEmailQuery("notfound@example.com");

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAccountByEmailQuery_IsCaseInsensitive()
    {
        // Arrange
        var account = new Account
        {
            Id = "account-case",
            Auth0UserId = "auth0|case",
            Email = "case@example.com",
            DisplayName = "CaseUser"
        };
        DbContext.Accounts.Add(account);
        await DbContext.SaveChangesAsync();

        var query = new GetAccountByEmailQuery("CASE@EXAMPLE.COM");

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be("case@example.com");
    }

    #endregion

    #region LinkProfilesToAccountCommand Tests

    [Fact]
    public async Task LinkProfilesToAccountCommand_WithValidData_LinksProfiles()
    {
        // Arrange
        var account = new Account
        {
            Id = "account-link",
            Auth0UserId = "auth0|link",
            Email = "link@example.com",
            DisplayName = "LinkUser",
            UserProfileIds = new List<string>()
        };
        DbContext.Accounts.Add(account);

        var profile1 = new UserProfile { Id = "profile-1", AccountId = "other" };
        var profile2 = new UserProfile { Id = "profile-2", AccountId = "other" };
        DbContext.UserProfiles.AddRange(profile1, profile2);

        await DbContext.SaveChangesAsync();

        var command = new LinkProfilesToAccountCommand("account-link", new List<string> { "profile-1", "profile-2" });

        // Act
        var result = await Mediator.Send(command);

        // Assert
        result.Should().BeTrue();

        var updatedAccount = await DbContext.Accounts.FindAsync("account-link");
        updatedAccount.Should().NotBeNull();
        updatedAccount!.UserProfileIds.Should().HaveCount(2);
        updatedAccount.UserProfileIds.Should().Contain("profile-1");
        updatedAccount.UserProfileIds.Should().Contain("profile-2");
    }

    #endregion
}
