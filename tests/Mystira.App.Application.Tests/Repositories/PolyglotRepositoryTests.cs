using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Infrastructure.Data.Polyglot;

namespace Mystira.App.Application.Tests.Repositories;

/// <summary>
/// Unit tests for PolyglotRepository dual-write behavior.
/// Tests verify migration phase handling and dual-write patterns.
/// </summary>
public class PolyglotRepositoryTests
{
    private readonly Mock<ILogger<PolyglotRepository<TestEntity>>> _loggerMock;

    public PolyglotRepositoryTests()
    {
        _loggerMock = new Mock<ILogger<PolyglotRepository<TestEntity>>>();
    }

    [Theory]
    [InlineData(MigrationPhase.CosmosOnly)]
    [InlineData(MigrationPhase.DualWriteCosmosRead)]
    [InlineData(MigrationPhase.DualWritePostgresRead)]
    [InlineData(MigrationPhase.PostgresOnly)]
    public void CurrentPhase_ShouldReturnConfiguredPhase(MigrationPhase expectedPhase)
    {
        // Arrange
        var options = new MigrationOptions { Phase = expectedPhase };
        using var context = CreateInMemoryContext();
        var sut = CreateRepository(context, options);

        // Act
        var actualPhase = sut.CurrentPhase;

        // Assert
        actualPhase.Should().Be(expectedPhase);
    }

    [Fact]
    public async Task AddAsync_InCosmosOnlyMode_ShouldOnlyWriteToPrimary()
    {
        // Arrange
        var options = new MigrationOptions { Phase = MigrationPhase.CosmosOnly };
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
        var options = new MigrationOptions { Phase = MigrationPhase.DualWriteCosmosRead };
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
        var options = new MigrationOptions { Phase = MigrationPhase.CosmosOnly };
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
        var options = new MigrationOptions { Phase = MigrationPhase.CosmosOnly };
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
        var options = new MigrationOptions { Phase = MigrationPhase.DualWriteCosmosRead };
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
        var options = new MigrationOptions { Phase = MigrationPhase.DualWriteCosmosRead };
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
        var options = new MigrationOptions { Phase = MigrationPhase.DualWriteCosmosRead };
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
        var options = new MigrationOptions { Phase = MigrationPhase.CosmosOnly };
        using var primaryContext = CreateInMemoryContext();
        var sut = CreateRepository(primaryContext, options, null);

        // Act
        var result = await sut.ValidateConsistencyAsync("1");

        // Assert
        result.IsConsistent.Should().BeTrue();
    }

    [Theory]
    [InlineData(BackendType.Primary)]
    [InlineData(BackendType.CosmosDb)]
    public async Task GetFromBackendAsync_ShouldReturnEntityFromCorrectBackend(BackendType backend)
    {
        // Arrange
        var options = new MigrationOptions { Phase = MigrationPhase.CosmosOnly };
        using var context = CreateInMemoryContext();

        var entity = new TestEntity { Id = "1", Name = "Test" };
        context.Set<TestEntity>().Add(entity);
        await context.SaveChangesAsync();

        var sut = CreateRepository(context, options);

        // Act
        var result = await sut.GetFromBackendAsync("1", backend);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("1");
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
        MigrationOptions options,
        DbContext? secondaryContext = null)
    {
        return new PolyglotRepository<TestEntity>(
            primaryContext,
            Options.Create(options),
            _loggerMock.Object,
            secondaryContext);
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
