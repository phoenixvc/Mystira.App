# Mystira.App.Application.Tests

Integration tests for CQRS command and query handlers in the Mystira.App application.

## Overview

This test project contains comprehensive integration tests for the CQRS implementation, including:
- Command handler tests (create, update, delete operations)
- Query handler tests (read operations)
- Caching behavior tests
- Specification pattern tests
- Full MediatR pipeline testing

## Test Structure

```
Mystira.App.Application.Tests/
├── CQRS/
│   ├── CqrsIntegrationTestBase.cs           # Base class for all CQRS tests
│   ├── BadgeConfigurations/
│   │   └── BadgeConfigurationQueryTests.cs  # Query + caching tests
│   ├── UserBadges/
│   │   ├── UserBadgeCommandTests.cs         # Command tests
│   │   └── UserBadgeQueryTests.cs           # Query tests
│   └── Scenarios/
│       └── ScenarioQueryTests.cs            # Scenario query tests
└── Mystira.App.Application.Tests.csproj
```

## Running Tests

### All Tests

```bash
# From repository root
dotnet test tests/Mystira.App.Application.Tests/

# With detailed output
dotnet test tests/Mystira.App.Application.Tests/ --verbosity detailed

# With coverage
dotnet test tests/Mystira.App.Application.Tests/ --collect:"XPlat Code Coverage"
```

### Specific Test Class

```bash
dotnet test tests/Mystira.App.Application.Tests/ --filter "FullyQualifiedName~BadgeConfigurationQueryTests"
```

### Specific Test Method

```bash
dotnet test tests/Mystira.App.Application.Tests/ --filter "FullyQualifiedName~GetAllBadgeConfigurationsQuery_UsesCaching"
```

### By Category (if using Traits)

```bash
dotnet test tests/Mystira.App.Application.Tests/ --filter "Category=Commands"
dotnet test tests/Mystira.App.Application.Tests/ --filter "Category=Queries"
dotnet test tests/Mystira.App.Application.Tests/ --filter "Category=Caching"
```

## Test Base Class

### CqrsIntegrationTestBase

All integration tests inherit from `CqrsIntegrationTestBase`, which provides:

- **In-memory database** - Fresh database per test class instance
- **Full MediatR pipeline** - Including caching behaviors
- **All repositories** - Registered with DI container
- **IMemoryCache** - For testing query caching
- **Cache invalidation service** - For testing cache management

#### Usage

```csharp
public class MyEntityTests : CqrsIntegrationTestBase
{
    // Override to seed test data
    protected override async Task SeedTestDataAsync()
    {
        DbContext.MyEntities.Add(new MyEntity { Id = "test-1", Name = "Test" });
        await DbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task MyTest()
    {
        await SeedTestDataAsync();

        var query = new GetMyEntityQuery("test-1");
        var result = await Mediator.Send(query);

        result.Should().NotBeNull();
    }
}
```

## Test Examples

### Testing a Query

```csharp
[Fact]
public async Task GetBadgeConfigurationQuery_ReturnsSingleBadge()
{
    // Arrange
    await SeedTestDataAsync();
    var query = new GetBadgeConfigurationQuery("badge-1");

    // Act
    var result = await Mediator.Send(query);

    // Assert
    result.Should().NotBeNull();
    result!.Id.Should().Be("badge-1");
    result.Name.Should().Be("Brave Heart");
}
```

### Testing Cache Behavior

```csharp
[Fact]
public async Task GetBadgeConfigurationQuery_UsesCaching()
{
    // Arrange
    await SeedTestDataAsync();
    var query = new GetBadgeConfigurationQuery("badge-1");

    // Act - First call (cache miss)
    var result1 = await Mediator.Send(query);

    // Modify database
    var badge = await DbContext.BadgeConfigurations.FindAsync("badge-1");
    badge!.Name = "Modified";
    await DbContext.SaveChangesAsync();

    // Act - Second call (cache hit, returns original)
    var result2 = await Mediator.Send(query);

    // Assert
    result1!.Name.Should().Be("Original");
    result2!.Name.Should().Be("Original"); // Still cached

    // Clear cache and verify fresh data
    ClearCache();
    var result3 = await Mediator.Send(query);
    result3!.Name.Should().Be("Modified");
}
```

### Testing a Command

```csharp
[Fact]
public async Task AwardBadgeCommand_CreatesNewBadge()
{
    // Arrange
    await SeedTestDataAsync();
    var request = new AwardBadgeRequest
    {
        UserProfileId = "profile-1",
        BadgeConfigurationId = "config-1",
        Axis = "Courage"
    };
    var command = new AwardBadgeCommand(request);

    // Act
    var result = await Mediator.Send(command);

    // Assert
    result.Should().NotBeNull();
    result.Id.Should().NotBeNullOrEmpty();
    result.UserProfileId.Should().Be("profile-1");

    // Verify persisted to database
    var saved = await DbContext.UserBadges.FindAsync(result.Id);
    saved.Should().NotBeNull();
}
```

### Testing Validation

```csharp
[Fact]
public async Task AwardBadgeCommand_WithMissingUserId_ThrowsException()
{
    // Arrange
    var request = new AwardBadgeRequest
    {
        UserProfileId = "", // Invalid
        BadgeConfigurationId = "config-1"
    };
    var command = new AwardBadgeCommand(request);

    // Act & Assert
    await Assert.ThrowsAsync<ArgumentException>(async () =>
    {
        await Mediator.Send(command);
    });
}
```

## Test Coverage

### BadgeConfigurationQueryTests

Tests for badge configuration queries with caching:

- ✅ `GetAllBadgeConfigurationsQuery_ReturnsAllBadges`
- ✅ `GetAllBadgeConfigurationsQuery_UsesCaching`
- ✅ `GetBadgeConfigurationQuery_ReturnsSingleBadge`
- ✅ `GetBadgeConfigurationQuery_WhenNotFound_ReturnsNull`
- ✅ `GetBadgeConfigurationQuery_UsesCaching`
- ✅ `GetBadgeConfigurationsByAxisQuery_ReturnsFilteredBadges`
- ✅ `GetBadgeConfigurationsByAxisQuery_WhenNoMatch_ReturnsEmpty`
- ✅ `MultipleConcurrentQueries_UseSeparateCacheKeys`

### UserBadgeCommandTests

Tests for user badge command handlers:

- ✅ `AwardBadgeCommand_CreatesNewBadge`
- ✅ `AwardBadgeCommand_WithMissingUserProfileId_ThrowsException`
- ✅ `AwardBadgeCommand_WithMissingBadgeConfigId_ThrowsException`
- ✅ `AwardBadgeCommand_CreatesUniqueIds`
- ✅ `AwardBadgeCommand_WithOptionalFields_SavesCorrectly`
- ✅ `AwardBadgeCommand_PersistsToDatabase`

### UserBadgeQueryTests

Tests for user badge query handlers:

- ✅ `GetUserBadgesQuery_ReturnsAllBadgesForUser`
- ✅ `GetUserBadgesQuery_OrdersByEarnedAtDescending`
- ✅ `GetUserBadgesQuery_WhenNoMatches_ReturnsEmpty`
- ✅ `GetUserBadgesQuery_DoesNotUseCaching`
- ✅ `GetUserBadgesQuery_IsolatesUserData`
- ✅ `GetUserBadgesByAxisQuery_FiltersCorrectly`
- ✅ `GetUserBadgesByAxisQuery_OrdersByEarnedAtDescending`
- ✅ `GetUserBadgesByAxisQuery_WhenNoMatches_ReturnsEmpty`
- ✅ `GetUserBadgesByAxisQuery_DoesNotCrossPollinate`

**Total Tests:** 23

## Best Practices

### 1. Use FluentAssertions

```csharp
// ✅ GOOD - Readable, clear intent
result.Should().NotBeNull();
result.Should().HaveCount(3);
result.Should().Contain(b => b.Name == "Test");

// ❌ BAD - Less readable
Assert.NotNull(result);
Assert.Equal(3, result.Count);
Assert.True(result.Any(b => b.Name == "Test"));
```

### 2. Arrange-Act-Assert Pattern

```csharp
[Fact]
public async Task MyTest()
{
    // Arrange - Set up test data and dependencies
    await SeedTestDataAsync();
    var query = new GetEntityQuery("id");

    // Act - Execute the operation
    var result = await Mediator.Send(query);

    // Assert - Verify expectations
    result.Should().NotBeNull();
}
```

### 3. Test One Thing Per Test

```csharp
// ✅ GOOD - Tests one specific behavior
[Fact]
public async Task Query_WhenNotFound_ReturnsNull()
{
    var query = new GetEntityQuery("non-existent");
    var result = await Mediator.Send(query);
    result.Should().BeNull();
}

// ❌ BAD - Tests multiple things
[Fact]
public async Task Query_VariousScenarios()
{
    // Tests found, not found, validation, caching...
}
```

### 4. Use Descriptive Test Names

Test names should describe:
- **What** is being tested
- **Under what conditions**
- **What the expected outcome is**

```csharp
// ✅ GOOD - Clear intent
GetUserBadgesQuery_WhenNoMatches_ReturnsEmpty()
AwardBadgeCommand_WithMissingUserId_ThrowsException()

// ❌ BAD - Unclear
TestQuery()
TestBadgeCommand()
```

### 5. Seed Only Required Data

```csharp
// ✅ GOOD - Seeds only what's needed for this test
protected override async Task SeedTestDataAsync()
{
    DbContext.BadgeConfigurations.Add(new BadgeConfiguration
    {
        Id = "badge-1",
        Name = "Test Badge",
        Axis = "Courage"
    });
    await DbContext.SaveChangesAsync();
}

// ❌ BAD - Seeds everything
protected override async Task SeedTestDataAsync()
{
    // Seeds 50+ entities that aren't used in most tests
}
```

### 6. Clear the DbContext for Fresh Queries

```csharp
// After modifying entities, clear tracker for fresh queries
DbContext.ChangeTracker.Clear();
var freshEntity = await DbContext.Entities.FindAsync(id);
```

## Troubleshooting

### Tests Fail with "Cannot access a disposed object"

**Cause:** Test is trying to use DbContext or ServiceProvider after disposal.

**Solution:** Ensure all async operations complete before test ends.

### Tests Fail with "Sequence contains no elements"

**Cause:** Query returned empty result when expecting data.

**Solution:** Verify `SeedTestDataAsync()` was called and data was saved.

### Cache Tests Not Working as Expected

**Cause:** Cache is shared across tests or not being cleared.

**Solution:** Each test class gets a new instance with fresh cache. Use `ClearCache()` when testing cache invalidation.

## Adding New Tests

### 1. Create Test Class

```csharp
public class MyEntityTests : CqrsIntegrationTestBase
{
    protected override async Task SeedTestDataAsync()
    {
        // Seed entity-specific test data
    }
}
```

### 2. Add Test Methods

```csharp
[Fact]
public async Task DescriptiveTestName()
{
    // Arrange
    await SeedTestDataAsync();

    // Act
    var result = await Mediator.Send(query);

    // Assert
    result.Should().NotBeNull();
}
```

### 3. Run and Verify

```bash
dotnet test tests/Mystira.App.Application.Tests/ --filter "FullyQualifiedName~MyEntityTests"
```

## Related Documentation

- [CQRS Migration Guide](../../docs/architecture/CQRS_MIGRATION_GUIDE.md)
- [Caching Strategy](../../docs/architecture/CACHING_STRATEGY.md)
- [ADR-0001: Adopt CQRS Pattern](../../docs/architecture/adr/ADR-0001-adopt-cqrs-pattern.md)
- [ADR-0006: Phase 5 - Complete CQRS Migration](../../docs/architecture/adr/ADR-0006-phase-5-cqrs-migration.md)

## CI/CD Integration

These tests are designed to run in CI/CD pipelines:

```yaml
# Example GitHub Actions
- name: Run Application Tests
  run: dotnet test tests/Mystira.App.Application.Tests/ --no-build --logger "trx;LogFileName=test-results.trx"

- name: Publish Test Results
  uses: EnricoMi/publish-unit-test-result-action@v2
  if: always()
  with:
    files: '**/test-results.trx'
```

---

**Last Updated:** 2025-11-24
**Test Coverage:** 23 tests covering Commands, Queries, and Caching

Copyright (c) 2025 Mystira. All rights reserved.
