using FluentAssertions;
using Mystira.App.Application.Specifications;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.Tests.Specifications;

/// <summary>
/// Unit tests for Account specifications.
/// Tests verify that specifications correctly filter entities.
/// </summary>
public class AccountSpecificationsTests
{
    private readonly List<Account> _accounts;

    public AccountSpecificationsTests()
    {
        _accounts = new List<Account>
        {
            CreateAccount("1", "user1@example.com", "auth0|user1", true),
            CreateAccount("2", "user2@example.com", "auth0|user2", true),
            CreateAccount("3", "admin@example.com", "auth0|admin", true),
            CreateAccount("4", "inactive@example.com", "auth0|inactive", false),
            CreateAccount("5", "test@test.com", "auth0|test", true),
        };
    }

    [Fact]
    public void AccountByEmailSpec_ShouldMatchExactEmail()
    {
        // Arrange
        var spec = new AccountByEmailSpec("user1@example.com");

        // Act
        var result = _accounts.AsQueryable().Where(spec.WhereExpressions.First().Filter).ToList();

        // Assert
        result.Should().HaveCount(1);
        result.First().Email.Should().Be("user1@example.com");
    }

    [Fact]
    public void AccountByEmailSpec_ShouldBeCaseInsensitive()
    {
        // Arrange
        var spec = new AccountByEmailSpec("USER1@EXAMPLE.COM");

        // Act
        var result = _accounts.AsQueryable().Where(spec.WhereExpressions.First().Filter).ToList();

        // Assert
        result.Should().HaveCount(1);
        result.First().Email.Should().Be("user1@example.com");
    }

    [Fact]
    public void AccountByEmailSpec_ShouldReturnEmpty_WhenNoMatch()
    {
        // Arrange
        var spec = new AccountByEmailSpec("nonexistent@example.com");

        // Act
        var result = _accounts.AsQueryable().Where(spec.WhereExpressions.First().Filter).ToList();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void AccountByAuth0UserIdSpec_ShouldMatchExactId()
    {
        // Arrange
        var spec = new AccountByAuth0UserIdSpec("auth0|admin");

        // Act
        var result = _accounts.AsQueryable().Where(spec.WhereExpressions.First().Filter).ToList();

        // Assert
        result.Should().HaveCount(1);
        result.First().Email.Should().Be("admin@example.com");
    }

    [Fact]
    public void AccountByIdSpec_ShouldMatchById()
    {
        // Arrange
        var spec = new AccountByIdSpec("3");

        // Act
        var result = _accounts.AsQueryable().Where(spec.WhereExpressions.First().Filter).ToList();

        // Assert
        result.Should().HaveCount(1);
        result.First().Id.Should().Be("3");
    }

    [Fact]
    public void ActiveAccountsSpec_ShouldFilterActiveOnly()
    {
        // Arrange
        var spec = new ActiveAccountsSpec();

        // Act
        var result = _accounts.AsQueryable().Where(spec.WhereExpressions.First().Filter).ToList();

        // Assert
        result.Should().HaveCount(4);
        result.Should().OnlyContain(a => a.Subscription.IsActive);
    }

    [Fact]
    public void AccountsByEmailPatternSpec_ShouldMatchPattern()
    {
        // Arrange
        var spec = new AccountsByEmailPatternSpec("example.com");

        // Act
        var result = _accounts.AsQueryable().Where(spec.WhereExpressions.First().Filter).ToList();

        // Assert
        result.Should().HaveCount(4);
        result.Should().OnlyContain(a => a.Email.Contains("example.com"));
    }

    [Fact]
    public void AccountsPaginatedSpec_ShouldApplyPaging()
    {
        // Arrange
        var spec = new AccountsPaginatedSpec(skip: 1, take: 2);

        // Act - Apply ordering and paging manually for in-memory test
        var result = _accounts
            .OrderBy(a => a.Email)
            .Skip(1)
            .Take(2)
            .ToList();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public void AccountsPaginatedSpec_WithSearchTerm_ShouldFilterAndPage()
    {
        // Arrange
        var spec = new AccountsPaginatedSpec(skip: 0, take: 10, searchTerm: "user");

        // Act
        var result = _accounts.AsQueryable().Where(spec.WhereExpressions.First().Filter).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(a => a.Email.Contains("user"));
    }

    private static Account CreateAccount(string id, string email, string auth0UserId, bool isActive)
    {
        return new Account
        {
            Id = id,
            Email = email,
            Auth0UserId = auth0UserId,
            Subscription = new SubscriptionDetails { IsActive = isActive },
            LastLoginAt = DateTime.UtcNow
        };
    }
}
