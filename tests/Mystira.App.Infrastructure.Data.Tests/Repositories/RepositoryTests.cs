using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Mystira.App.Domain.Models;
using Mystira.App.Infrastructure.Data;
using Mystira.App.Infrastructure.Data.Repositories;

namespace Mystira.App.Infrastructure.Data.Tests.Repositories;

public class RepositoryTests : IDisposable
{
    private readonly MystiraAppDbContext _context;
    private readonly Repository<BadgeConfiguration> _repository;

    public RepositoryTests()
    {
        var options = new DbContextOptionsBuilder<MystiraAppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new MystiraAppDbContext(options);
        _repository = new Repository<BadgeConfiguration>(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingEntity_ReturnsEntity()
    {
        // Arrange
        var entity = CreateTestBadgeConfig();
        await _context.Set<BadgeConfiguration>().AddAsync(entity);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(entity.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(entity.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingEntity_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(Guid.NewGuid().ToString());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_WithEntities_ReturnsAllEntities()
    {
        // Arrange
        var entity1 = CreateTestBadgeConfig();
        var entity2 = CreateTestBadgeConfig();
        await _context.Set<BadgeConfiguration>().AddRangeAsync(entity1, entity2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task FindAsync_WithMatchingPredicate_ReturnsMatchingEntities()
    {
        // Arrange
        var targetAxis = "courage";
        var entity1 = CreateTestBadgeConfig(axis: targetAxis);
        var entity2 = CreateTestBadgeConfig(axis: targetAxis);
        var entity3 = CreateTestBadgeConfig(axis: "wisdom");
        await _context.Set<BadgeConfiguration>().AddRangeAsync(entity1, entity2, entity3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.FindAsync(e => e.Axis == targetAxis);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(e => e.Axis == targetAxis);
    }

    [Fact]
    public async Task AddAsync_WithValidEntity_AddsToContext()
    {
        // Arrange
        var entity = CreateTestBadgeConfig();

        // Act
        var result = await _repository.AddAsync(entity);
        await _context.SaveChangesAsync();

        // Assert
        result.Should().NotBeNull();
        var stored = await _context.Set<BadgeConfiguration>().FindAsync(entity.Id);
        stored.Should().NotBeNull();
    }

    [Fact]
    public async Task AddAsync_WithNullEntity_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _repository.AddAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task UpdateAsync_WithExistingEntity_UpdatesEntity()
    {
        // Arrange
        var entity = CreateTestBadgeConfig();
        await _context.Set<BadgeConfiguration>().AddAsync(entity);
        await _context.SaveChangesAsync();
        _context.Entry(entity).State = EntityState.Detached;

        entity.Name = "Updated Name";

        // Act
        await _repository.UpdateAsync(entity);
        await _context.SaveChangesAsync();

        // Assert
        var updated = await _context.Set<BadgeConfiguration>().FindAsync(entity.Id);
        updated!.Name.Should().Be("Updated Name");
    }

    [Fact]
    public async Task DeleteAsync_WithExistingEntity_RemovesEntity()
    {
        // Arrange
        var entity = CreateTestBadgeConfig();
        await _context.Set<BadgeConfiguration>().AddAsync(entity);
        await _context.SaveChangesAsync();

        // Act
        await _repository.DeleteAsync(entity.Id);
        await _context.SaveChangesAsync();

        // Assert
        var deleted = await _context.Set<BadgeConfiguration>().FindAsync(entity.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task ExistsAsync_WithExistingEntity_ReturnsTrue()
    {
        // Arrange
        var entity = CreateTestBadgeConfig();
        await _context.Set<BadgeConfiguration>().AddAsync(entity);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.ExistsAsync(entity.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WithNonExistingEntity_ReturnsFalse()
    {
        // Act
        var result = await _repository.ExistsAsync(Guid.NewGuid().ToString());

        // Assert
        result.Should().BeFalse();
    }

    private static BadgeConfiguration CreateTestBadgeConfig(string? axis = null)
    {
        return new BadgeConfiguration
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Test Badge",
            Axis = axis ?? "default",
            Threshold = 3.0f,
            Message = "Test message"
        };
    }
}
