# Test Templates

This directory contains example test files demonstrating best practices for testing different layers of the Mystira.App project.

## Templates

### 1. DomainModelTest.cs
**Purpose:** Testing domain entities and business logic

**Key Patterns:**
- Constructor validation
- Property setters/getters
- Domain methods
- Business rule enforcement
- Invariant validation

**When to use:** Creating tests for entities in `Mystira.App.Domain/Models/`

---

### 2. CqrsCommandTest.cs
**Purpose:** Testing CQRS command handlers

**Key Patterns:**
- Inherits from `CqrsIntegrationTestBase`
- Uses in-memory database
- Seeds test data
- Verifies persistence
- Tests validation errors
- Tests cache invalidation

**When to use:** Creating tests for commands in `Mystira.App.Application/CQRS/{Feature}/Commands/`

---

### 3. CqrsQueryTest.cs
**Purpose:** Testing CQRS query handlers

**Key Patterns:**
- Inherits from `CqrsIntegrationTestBase`
- Tests filtering and sorting
- Verifies caching behavior
- Tests cache invalidation
- Tests null returns

**When to use:** Creating tests for queries in `Mystira.App.Application/CQRS/{Feature}/Queries/`

---

### 4. ApiControllerTest.cs
**Purpose:** Testing API controllers

**Key Patterns:**
- Uses Moq to mock dependencies
- Tests HTTP status codes (200, 404, 400, 500)
- Tests response shapes
- Tests authorization attributes
- Tests error handling
- Verifies MediatR calls

**When to use:** Creating tests for controllers in `Mystira.App.Api/Controllers/`

---

### 5. ServiceTest.cs
**Purpose:** Testing application services

**Key Patterns:**
- Mocks external dependencies
- Tests service orchestration
- Tests event-based services
- Verifies method calls
- Tests error handling

**When to use:** Creating tests for services in `Mystira.App.{Layer}/Services/`

---

### 6. BlazorComponentTest.cs
**Purpose:** Testing Blazor components (using bUnit)

**Key Patterns:**
- Uses `TestContext` from bUnit
- Tests component rendering
- Tests parameter binding
- Tests user interactions (clicks, input)
- Tests event callbacks
- Tests component lifecycle
- Verifies accessibility attributes

**When to use:** Creating tests for components in `Mystira.App.PWA/Components/` or `Mystira.App.PWA/Pages/`

---

## Quick Start

### Copy a template
```bash
# Domain model test
cp docs/templates/tests/DomainModelTest.cs tests/Mystira.App.Domain.Tests/Models/YourEntityTests.cs

# CQRS command test
cp docs/templates/tests/CqrsCommandTest.cs tests/Mystira.App.Application.Tests/CQRS/YourFeature/YourCommandTests.cs

# API controller test
cp docs/templates/tests/ApiControllerTest.cs tests/Mystira.App.Api.Tests/Controllers/YourControllerTests.cs

# Blazor component test
cp docs/templates/tests/BlazorComponentTest.cs tests/Mystira.App.PWA.Tests/Components/YourComponentTests.cs
```

### Find and replace placeholders
- `{EntityName}` → Your entity name (e.g., `Scenario`, `GameSession`)
- `{Feature}` → Your feature area (e.g., `Scenarios`, `GameSessions`)
- `{CommandName}` → Your command name (e.g., `CreateScenarioCommand`)
- `{QueryType}` → Your query name (e.g., `GetScenarioQuery`)
- `{ControllerName}` → Your controller name (e.g., `ScenariosController`)
- `{ComponentName}` → Your component name (e.g., `LoadingIndicator`)

### Run your tests
```bash
# Run all tests in a project
dotnet test tests/Mystira.App.Domain.Tests/

# Run a specific test file
dotnet test --filter "FullyQualifiedName~YourEntityTests"

# Run a specific test
dotnet test --filter "FullyQualifiedName~YourEntityTests.YourTestMethod"
```

---

## Testing Patterns

### Arrange-Act-Assert (AAA)
All tests follow the AAA pattern:

```csharp
[Fact]
public void Method_WhenCondition_ExpectedOutcome()
{
    // Arrange - Set up test data and dependencies
    var entity = new Entity { Property = "value" };

    // Act - Execute the method being tested
    var result = entity.Method();

    // Assert - Verify the outcome
    result.Should().Be(expectedValue);
}
```

### Test Naming Convention
- **Pattern:** `Method_WhenCondition_ExpectedOutcome`
- **Examples:**
  - `CreateScenario_WithValidRequest_CreatesScenario`
  - `GetById_WhenEntityNotFound_ReturnsNull`
  - `ShowSuccess_RaisesOnShowEvent`

### FluentAssertions
Use FluentAssertions for readable, maintainable assertions:

```csharp
// ✅ Good - Readable and clear
result.Should().NotBeNull();
result.Title.Should().Be("Expected Title");
result.Scenes.Should().HaveCount(3);

// ❌ Avoid - Less readable
Assert.NotNull(result);
Assert.Equal("Expected Title", result.Title);
Assert.Equal(3, result.Scenes.Count);
```

---

## Dependencies

### Required NuGet Packages

**All Test Projects:**
- `xunit` (2.6.0+)
- `xunit.runner.visualstudio` (2.5.0+)
- `FluentAssertions` (6.12.0+)
- `Microsoft.NET.Test.Sdk` (17.8.0+)

**API/Application Tests:**
- `Moq` (4.20.0+)
- `Microsoft.EntityFrameworkCore.InMemory` (9.0.0+)

**Blazor Component Tests:**
- `bUnit` (1.25.0+)
- `bUnit.web` (1.25.0+)

**Optional:**
- `AutoFixture.Xunit2` (4.18.0+) - For test data generation
- `Coverlet.Collector` (6.0.0+) - For code coverage

---

## Best Practices

### DO ✅
1. Test one thing per test
2. Use descriptive test names
3. Keep tests simple and focused
4. Test edge cases and error conditions
5. Use FluentAssertions
6. Clean up resources (dispose, reset state)
7. Make tests independent (no shared state)

### DON'T ❌
1. Don't test implementation details
2. Don't use `Thread.Sleep` (use async/await)
3. Don't share state between tests
4. Don't mock everything
5. Don't ignore failing tests
6. Don't use magic values
7. Don't test framework code

---

## More Information

- **Testing Strategy:** See `docs/TESTING_STRATEGY.md`
- **Existing Tests:** See `tests/` directory
- **CI/CD:** Tests run automatically on every PR
- **Coverage Reports:** Available in Azure DevOps after PR build

---

**Questions?**
Check the existing tests in `tests/` for more examples, or refer to the comprehensive testing strategy document.
