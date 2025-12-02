using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Mystira.App.Domain.Models;
using Mystira.App.Infrastructure.Data.Repositories;
using Xunit;

namespace Mystira.App.Api.Tests.Repositories;

public class PendingSignupRepositoryTests
{
    [Fact]
    public async Task GetByEmailAndCodeAsync_WhenMatchingUnusedSignupExists_ReturnsSignup()
    {
        // Arrange
        var testData = new List<PendingSignup>
        {
            new PendingSignup
            {
                Id = Guid.NewGuid().ToString(),
                Email = "test@example.com",
                Code = "123456",
                DisplayName = "Test User",
                IsUsed = false,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15)
            }
        };

        var mockDbSet = CreateMockDbSet(testData);
        var mockContext = new Mock<DbContext>();
        mockContext.Setup(c => c.Set<PendingSignup>()).Returns(mockDbSet.Object);

        var repository = new PendingSignupRepository(mockContext.Object);

        // Act
        var result = await repository.GetByEmailAndCodeAsync("test@example.com", "123456");

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be("test@example.com");
        result.Code.Should().Be("123456");
    }

    [Fact]
    public async Task GetByEmailAndCodeAsync_WhenSignupIsUsed_ReturnsNull()
    {
        // Arrange
        var testData = new List<PendingSignup>
        {
            new PendingSignup
            {
                Id = Guid.NewGuid().ToString(),
                Email = "test@example.com",
                Code = "123456",
                DisplayName = "Test User",
                IsUsed = true,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15)
            }
        };

        var mockDbSet = CreateMockDbSet(testData);
        var mockContext = new Mock<DbContext>();
        mockContext.Setup(c => c.Set<PendingSignup>()).Returns(mockDbSet.Object);

        var repository = new PendingSignupRepository(mockContext.Object);

        // Act
        var result = await repository.GetByEmailAndCodeAsync("test@example.com", "123456");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByEmailAndCodeAsync_WhenCodeDoesNotMatch_ReturnsNull()
    {
        // Arrange
        var testData = new List<PendingSignup>
        {
            new PendingSignup
            {
                Id = Guid.NewGuid().ToString(),
                Email = "test@example.com",
                Code = "123456",
                DisplayName = "Test User",
                IsUsed = false,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15)
            }
        };

        var mockDbSet = CreateMockDbSet(testData);
        var mockContext = new Mock<DbContext>();
        mockContext.Setup(c => c.Set<PendingSignup>()).Returns(mockDbSet.Object);

        var repository = new PendingSignupRepository(mockContext.Object);

        // Act
        var result = await repository.GetByEmailAndCodeAsync("test@example.com", "654321");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByEmailAndCodeAsync_WhenEmailDoesNotMatch_ReturnsNull()
    {
        // Arrange
        var testData = new List<PendingSignup>
        {
            new PendingSignup
            {
                Id = Guid.NewGuid().ToString(),
                Email = "test@example.com",
                Code = "123456",
                DisplayName = "Test User",
                IsUsed = false,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15)
            }
        };

        var mockDbSet = CreateMockDbSet(testData);
        var mockContext = new Mock<DbContext>();
        mockContext.Setup(c => c.Set<PendingSignup>()).Returns(mockDbSet.Object);

        var repository = new PendingSignupRepository(mockContext.Object);

        // Act
        var result = await repository.GetByEmailAndCodeAsync("other@example.com", "123456");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetActiveByEmailAsync_WhenActiveSignupExists_ReturnsSignup()
    {
        // Arrange
        var testData = new List<PendingSignup>
        {
            new PendingSignup
            {
                Id = Guid.NewGuid().ToString(),
                Email = "test@example.com",
                Code = "123456",
                DisplayName = "Test User",
                IsUsed = false,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15)
            }
        };

        var mockDbSet = CreateMockDbSet(testData);
        var mockContext = new Mock<DbContext>();
        mockContext.Setup(c => c.Set<PendingSignup>()).Returns(mockDbSet.Object);

        var repository = new PendingSignupRepository(mockContext.Object);

        // Act
        var result = await repository.GetActiveByEmailAsync("test@example.com");

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task GetActiveByEmailAsync_WhenSignupIsUsed_ReturnsNull()
    {
        // Arrange
        var testData = new List<PendingSignup>
        {
            new PendingSignup
            {
                Id = Guid.NewGuid().ToString(),
                Email = "test@example.com",
                Code = "123456",
                DisplayName = "Test User",
                IsUsed = true,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15)
            }
        };

        var mockDbSet = CreateMockDbSet(testData);
        var mockContext = new Mock<DbContext>();
        mockContext.Setup(c => c.Set<PendingSignup>()).Returns(mockDbSet.Object);

        var repository = new PendingSignupRepository(mockContext.Object);

        // Act
        var result = await repository.GetActiveByEmailAsync("test@example.com");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetActiveByEmailAsync_WhenSignupIsExpired_ReturnsNull()
    {
        // Arrange
        var testData = new List<PendingSignup>
        {
            new PendingSignup
            {
                Id = Guid.NewGuid().ToString(),
                Email = "test@example.com",
                Code = "123456",
                DisplayName = "Test User",
                IsUsed = false,
                ExpiresAt = DateTime.UtcNow.AddMinutes(-15)
            }
        };

        var mockDbSet = CreateMockDbSet(testData);
        var mockContext = new Mock<DbContext>();
        mockContext.Setup(c => c.Set<PendingSignup>()).Returns(mockDbSet.Object);

        var repository = new PendingSignupRepository(mockContext.Object);

        // Act
        var result = await repository.GetActiveByEmailAsync("test@example.com");

        // Assert
        result.Should().BeNull();
    }

    private static Mock<DbSet<T>> CreateMockDbSet<T>(List<T> data) where T : class
    {
        var queryable = data.AsQueryable();
        var mockDbSet = new Mock<DbSet<T>>();

        mockDbSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(new TestAsyncQueryProvider<T>(queryable.Provider));
        mockDbSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
        mockDbSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
        mockDbSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(queryable.GetEnumerator());
        mockDbSet.As<IAsyncEnumerable<T>>().Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(new TestAsyncEnumerator<T>(data.GetEnumerator()));

        return mockDbSet;
    }
}

internal class TestAsyncQueryProvider<T> : IAsyncQueryProvider
{
    private readonly IQueryProvider _inner;

    internal TestAsyncQueryProvider(IQueryProvider inner)
    {
        _inner = inner;
    }

    public IQueryable CreateQuery(System.Linq.Expressions.Expression expression)
    {
        return new TestAsyncEnumerable<T>(expression);
    }

    public IQueryable<TElement> CreateQuery<TElement>(System.Linq.Expressions.Expression expression)
    {
        return new TestAsyncEnumerable<TElement>(expression);
    }

    public object? Execute(System.Linq.Expressions.Expression expression)
    {
        return _inner.Execute(expression);
    }

    public TResult Execute<TResult>(System.Linq.Expressions.Expression expression)
    {
        return _inner.Execute<TResult>(expression);
    }

    public TResult ExecuteAsync<TResult>(System.Linq.Expressions.Expression expression, CancellationToken cancellationToken = default)
    {
        var expectedResultType = typeof(TResult).GetGenericArguments()[0];
        var executionResult = typeof(IQueryProvider)
            .GetMethod(
                name: nameof(IQueryProvider.Execute),
                genericParameterCount: 1,
                types: new[] { typeof(System.Linq.Expressions.Expression) })!
            .MakeGenericMethod(expectedResultType)
            .Invoke(this, new[] { expression });

        return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))!
            .MakeGenericMethod(expectedResultType)
            .Invoke(null, new[] { executionResult })!;
    }
}

internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    public TestAsyncEnumerable(IEnumerable<T> enumerable)
        : base(enumerable)
    { }

    public TestAsyncEnumerable(System.Linq.Expressions.Expression expression)
        : base(expression)
    { }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        return new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
    }

    IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
}

internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _inner;

    public TestAsyncEnumerator(IEnumerator<T> inner)
    {
        _inner = inner;
    }

    public ValueTask DisposeAsync()
    {
        _inner.Dispose();
        return ValueTask.CompletedTask;
    }

    public ValueTask<bool> MoveNextAsync()
    {
        return new ValueTask<bool>(_inner.MoveNext());
    }

    public T Current => _inner.Current;
}
