using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Mystira.App.Infrastructure.Data.Polyglot;
using Mystira.Shared.Polyglot;
using Mystira.Shared.Telemetry;

namespace Mystira.App.Application.Tests.Repositories;

/// <summary>
/// Unit tests for PolyglotRepository dual-write behavior.
/// Tests verify polyglot mode handling and dual-write patterns.
/// </summary>
public class PolyglotRepositoryTests
{
    private readonly Mock<ILogger<PolyglotRepository<TestEntity>>> _loggerMock;
    private readonly Mock<ICustomMetrics> _metricsMock;

    public PolyglotRepositoryTests()
    {
        _loggerMock = new Mock<ILogger<PolyglotRepository<TestEntity>>>();
        _metricsMock = new Mock<ICustomMetrics>();
    }

    [Theory]
    [InlineData(PolyglotMode.SingleStore)]
    [InlineData(PolyglotMode.DualWrite)]
    public void CurrentMode_ShouldReturnConfiguredMode(PolyglotMode expectedMode)
    {
        // Arrange
        var options = new PolyglotOptions { Mode = expectedMode };
        using var context = CreateInMemoryContext();
        var sut = CreateRepository(context, options);

        // Act
        var actualMode = sut.CurrentMode;

        // Assert
        actualMode.Should().Be(expectedMode);
    }

    [Fact]
    public async Task AddAsync_InSingleStoreMode_ShouldOnlyWriteToPrimary()
    {
        // Arrange
        var options = new PolyglotOptions { Mode = PolyglotMode.SingleStore };
        using var primaryContext = CreateInMemoryContext("primary");
        using var secondaryContext = CreateInMemoryContext("secondary");

        var sut = CreateRepository(primaryContext, options, secondaryContext);
        var entity = new TestEntity { Id = "1", Name = "Test" };

        // Act
        var result = await sut.AddAsync(entity);

        // Assert
        result.Should().NotBeNull();
        primaryContext.Set<TestEntity>().Count().Should().Be(1);
        secondaryContext.Set<TestEntity>().Count().Should().Be(0);
    }

    [Fact]
    public async Task AddAsync_InDualWriteMode_ShouldWriteToBothContexts()
    {
        // Arrange
        var options = new PolyglotOptions { Mode = PolyglotMode.DualWrite };
        using var primaryContext = CreateInMemoryContext("primary");
        using var secondaryContext = CreateInMemoryContext("secondary");

        var sut = CreateRepository(primaryContext, options, secondaryContext);
        var entity = new TestEntity { Id = "1", Name = "Test" };

        // Act
        var result = await sut.AddAsync(entity);

        // Assert
        result.Should().NotBeNull();
        primaryContext.Set<TestEntity>().Count().Should().Be(1);
        secondaryContext.Set<TestEntity>().Count().Should().Be(1);
    }

    [Fact]
    public async Task IsPrimaryHealthyAsync_WhenConnectable_ShouldReturnTrue()
    {
        // Arrange
        var options = new PolyglotOptions { Mode = PolyglotMode.SingleStore };
        using var context = CreateInMemoryContext();
        var sut = CreateRepository(context, options);

        // Act
        var result = await sut.IsPrimaryHealthyAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsSecondaryHealthyAsync_WhenNoSecondary_ShouldReturnFalse()
    {
        // Arrange
        var options = new PolyglotOptions { Mode = PolyglotMode.SingleStore };
        using var context = CreateInMemoryContext();
        var sut = CreateRepository(context, options, null);

        // Act
        var result = await sut.IsSecondaryHealthyAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsSecondaryHealthyAsync_WhenSecondaryConnectable_ShouldReturnTrue()
    {
        // Arrange
        var options = new PolyglotOptions { Mode = PolyglotMode.DualWrite };
        using var primaryContext = CreateInMemoryContext("primary");
        using var secondaryContext = CreateInMemoryContext("secondary");
        var sut = CreateRepository(primaryContext, options, secondaryContext);

        // Act
        var result = await sut.IsSecondaryHealthyAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateConsistencyAsync_WhenBothContextsHaveSameData_ShouldReturnConsistent()
    {
        // Arrange
        var options = new PolyglotOptions { Mode = PolyglotMode.DualWrite };
        using var primaryContext = CreateInMemoryContext("primary");
        using var secondaryContext = CreateInMemoryContext("secondary");

        var entity = new TestEntity { Id = "1", Name = "Test" };
        primaryContext.Set<TestEntity>().Add(entity);
        await primaryContext.SaveChangesAsync();

        var entityCopy = new TestEntity { Id = "1", Name = "Test" };
        secondaryContext.Set<TestEntity>().Add(entityCopy);
        await secondaryContext.SaveChangesAsync();

        var sut = CreateRepository(primaryContext, options, secondaryContext);

        // Act
        var result = await sut.ValidateConsistencyAsync("1");

        // Assert
        result.IsConsistent.Should().BeTrue();
        result.Differences.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateConsistencyAsync_WhenMissingInSecondary_ShouldReturnInconsistent()
    {
        // Arrange
        var options = new PolyglotOptions { Mode = PolyglotMode.DualWrite };
        using var primaryContext = CreateInMemoryContext("primary");
        using var secondaryContext = CreateInMemoryContext("secondary");

        var entity = new TestEntity { Id = "1", Name = "Test" };
        primaryContext.Set<TestEntity>().Add(entity);
        await primaryContext.SaveChangesAsync();

        var sut = CreateRepository(primaryContext, options, secondaryContext);

        // Act
        var result = await sut.ValidateConsistencyAsync("1");

        // Assert
        result.IsConsistent.Should().BeFalse();
        result.Differences.Should().Contain("Missing in secondary");
    }

    [Fact]
    public async Task ValidateConsistencyAsync_WhenNoSecondaryContext_ShouldReturnConsistent()
    {
        // Arrange
        var options = new PolyglotOptions { Mode = PolyglotMode.SingleStore };
        using var primaryContext = CreateInMemoryContext();
        var sut = CreateRepository(primaryContext, options, null);

        // Act
        var result = await sut.ValidateConsistencyAsync("1");

        // Assert
        result.IsConsistent.Should().BeTrue();
    }

    [Fact]
    public async Task GetFromBackendAsync_Primary_ShouldReturnEntityFromPrimaryContext()
    {
        // Arrange
        var options = new PolyglotOptions { Mode = PolyglotMode.SingleStore };
        using var primaryContext = CreateInMemoryContext("primary");

        var entity = new TestEntity { Id = "1", Name = "Test" };
        primaryContext.Set<TestEntity>().Add(entity);
        await primaryContext.SaveChangesAsync();

        var sut = CreateRepository(primaryContext, options);

        // Act
        var result = await sut.GetFromBackendAsync("1", BackendType.Primary);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("1");
    }

    [Fact]
    public async Task GetFromBackendAsync_Secondary_ShouldReturnEntityFromSecondaryContext()
    {
        // Arrange
        var options = new PolyglotOptions { Mode = PolyglotMode.DualWrite };
        using var primaryContext = CreateInMemoryContext("primary");
        using var secondaryContext = CreateInMemoryContext("secondary");

        // Add entity to secondary context only
        var entity = new TestEntity { Id = "2", Name = "SecondaryOnly" };
        secondaryContext.Set<TestEntity>().Add(entity);
        await secondaryContext.SaveChangesAsync();

        var sut = CreateRepository(primaryContext, options, secondaryContext);

        // Act
        var result = await sut.GetFromBackendAsync("2", BackendType.Secondary);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("2");
        result.Name.Should().Be("SecondaryOnly");
    }

    [Fact]
    public async Task GetFromBackendAsync_Secondary_WhenNoSecondaryContext_ShouldReturnNull()
    {
        // Arrange
        var options = new PolyglotOptions { Mode = PolyglotMode.SingleStore };
        using var primaryContext = CreateInMemoryContext();

        var sut = CreateRepository(primaryContext, options, null);

        // Act
        var result = await sut.GetFromBackendAsync("1", BackendType.Secondary);

        // Assert
        result.Should().BeNull();
    }

    private TestDbContext CreateInMemoryContext(string? name = null)
    {
        // Always append a unique GUID to ensure test isolation even when using named databases
        var databaseName = name != null ? $"{name}_{Guid.NewGuid()}" : Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: databaseName)
            .Options;

        return new TestDbContext(options);
    }

    private PolyglotRepository<TestEntity> CreateRepository(
        DbContext primaryContext,
        PolyglotOptions options,
        DbContext? secondaryContext = null)
    {
        return new PolyglotRepository<TestEntity>(
            primaryContext,
            Options.Create(options),
            _loggerMock.Object,
            secondaryContext,
            _metricsMock.Object);
    }

    public class TestEntity
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

        public DbSet<TestEntity> TestEntities => Set<TestEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TestEntity>().HasKey(e => e.Id);
        }
    }
}
