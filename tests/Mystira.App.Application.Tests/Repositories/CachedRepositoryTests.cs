using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Infrastructure.Data.Caching;
using System.Text;
using System.Text.Json;

namespace Mystira.App.Application.Tests.Repositories;

/// <summary>
/// Unit tests for CachedRepository decorator.
/// Tests verify cache-aside pattern behavior.
/// </summary>
public class CachedRepositoryTests
{
    private readonly Mock<ISpecRepository<TestEntity>> _innerRepoMock;
    private readonly Mock<IDistributedCache> _cacheMock;
    private readonly Mock<ILogger<CachedRepository<TestEntity>>> _loggerMock;
    private readonly CacheOptions _cacheOptions;
    private readonly CachedRepository<TestEntity> _sut;

    public CachedRepositoryTests()
    {
        _innerRepoMock = new Mock<ISpecRepository<TestEntity>>();
        _cacheMock = new Mock<IDistributedCache>();
        _loggerMock = new Mock<ILogger<CachedRepository<TestEntity>>>();
        _cacheOptions = new CacheOptions
        {
            Enabled = true,
            KeyPrefix = "test:",
            DefaultSlidingExpirationMinutes = 30,
            DefaultAbsoluteExpirationMinutes = 60,
            EnableWriteThrough = true,
            EnableInvalidationOnChange = true
        };

        _sut = new CachedRepository<TestEntity>(
            _innerRepoMock.Object,
            _cacheMock.Object,
            Options.Create(_cacheOptions),
            _loggerMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCacheHit_ShouldReturnFromCache()
    {
        // Arrange
        var entity = new TestEntity { Id = "123", Name = "Test" };
        var cachedJson = JsonSerializer.Serialize(entity, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Encoding.UTF8.GetBytes(cachedJson));

        // Act
        var result = await _sut.GetByIdAsync("123");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("123");
        _innerRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCacheMiss_ShouldQueryInnerRepo()
    {
        // Arrange
        var entity = new TestEntity { Id = "123", Name = "Test" };
        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);
        _innerRepoMock.Setup(r => r.GetByIdAsync("123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        // Act
        var result = await _sut.GetByIdAsync("123");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("123");
        _innerRepoMock.Verify(r => r.GetByIdAsync("123", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCacheMissAndFound_ShouldPopulateCache()
    {
        // Arrange
        var entity = new TestEntity { Id = "123", Name = "Test" };
        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);
        _innerRepoMock.Setup(r => r.GetByIdAsync("123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        // Act
        await _sut.GetByIdAsync("123");

        // Assert
        _cacheMock.Verify(c => c.SetAsync(
            It.Is<string>(k => k.Contains("testentity:123")),
            It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCacheDisabled_ShouldBypassCache()
    {
        // Arrange
        var disabledOptions = new CacheOptions { Enabled = false };
        var sut = new CachedRepository<TestEntity>(
            _innerRepoMock.Object,
            _cacheMock.Object,
            Options.Create(disabledOptions),
            _loggerMock.Object);

        var entity = new TestEntity { Id = "123", Name = "Test" };
        _innerRepoMock.Setup(r => r.GetByIdAsync("123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        // Act
        var result = await sut.GetByIdAsync("123");

        // Assert
        result.Should().NotBeNull();
        _cacheMock.Verify(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _innerRepoMock.Verify(r => r.GetByIdAsync("123", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddAsync_ShouldDelegateToInnerRepo()
    {
        // Arrange
        var entity = new TestEntity { Id = "123", Name = "Test" };
        _innerRepoMock.Setup(r => r.AddAsync(entity, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        // Act
        var result = await _sut.AddAsync(entity);

        // Assert
        result.Should().Be(entity);
        _innerRepoMock.Verify(r => r.AddAsync(entity, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ShouldInvalidateCache()
    {
        // Arrange
        var entity = new TestEntity { Id = "123", Name = "Updated" };
        _innerRepoMock.Setup(r => r.UpdateAsync(entity, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _sut.UpdateAsync(entity);

        // Assert
        _innerRepoMock.Verify(r => r.UpdateAsync(entity, It.IsAny<CancellationToken>()), Times.Once);
        // With EnableWriteThrough, it should update cache, not remove it
        _cacheMock.Verify(c => c.SetAsync(
            It.Is<string>(k => k.Contains("testentity:123")),
            It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldInvalidateCache()
    {
        // Arrange
        var entity = new TestEntity { Id = "123", Name = "ToDelete" };
        _innerRepoMock.Setup(r => r.DeleteAsync(entity, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _sut.DeleteAsync(entity);

        // Assert
        _innerRepoMock.Verify(r => r.DeleteAsync(entity, It.IsAny<CancellationToken>()), Times.Once);
        _cacheMock.Verify(c => c.RemoveAsync(
            It.Is<string>(k => k.Contains("testentity:123")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExistsAsync_WhenCacheHit_ShouldReturnTrue()
    {
        // Arrange
        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Encoding.UTF8.GetBytes("{}"));

        // Act
        var result = await _sut.ExistsAsync("123");

        // Assert
        result.Should().BeTrue();
        _innerRepoMock.Verify(r => r.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExistsAsync_WhenCacheMiss_ShouldQueryInnerRepo()
    {
        // Arrange
        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);
        _innerRepoMock.Setup(r => r.ExistsAsync("123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.ExistsAsync("123");

        // Assert
        result.Should().BeTrue();
        _innerRepoMock.Verify(r => r.ExistsAsync("123", It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Test entity implementing IHasId for cache key generation tests.
    /// </summary>
    public class TestEntity : IHasId
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}
