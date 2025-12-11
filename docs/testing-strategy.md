# Testing Strategy

**Created:** November 25, 2025
**Version:** 1.0
**Target Coverage:** 60%+ overall, 80%+ critical paths, 90%+ domain layer

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Current State](#current-state)
3. [Testing Philosophy](#testing-philosophy)
4. [Test Pyramid](#test-pyramid)
5. [Coverage Goals](#coverage-goals)
6. [Testing Patterns](#testing-patterns)
7. [Test Templates](#test-templates)
8. [Critical Test Paths](#critical-test-paths)
9. [Tools & Frameworks](#tools--frameworks)
10. [CI/CD Integration](#cicd-integration)
11. [Best Practices](#best-practices)

---

## Executive Summary

This document outlines the comprehensive testing strategy for the Mystira.App project, a children's storytelling platform targeting ages 5-12. The strategy prioritizes:

- **Quality over quantity**: Focus on critical paths and business logic
- **Fast feedback loops**: Unit tests run in < 5s, integration tests < 30s
- **Maintainability**: Clear patterns, minimal mocking, readable tests
- **Safety**: High coverage for domain logic, COPPA compliance, and user data

**Current Status:**
- Test coverage: ~3.7% (22 test files / 591 source files)
- Test projects: 7 (API, Application, Domain, Infrastructure)
- Framework: xUnit + FluentAssertions + Moq + In-Memory EF Core

**Target Status (Q1 2026):**
- Test coverage: 60%+ overall
- Critical path coverage: 80%+
- Domain layer coverage: 90%+
- Automated test runs on every PR

---

## Current State

### Existing Test Projects

```
tests/
├── Mystira.App.Api.Tests/                    # API controller tests
├── Mystira.App.Application.Tests/            # CQRS command/query tests
├── Mystira.App.Admin.Api.Tests/              # Admin API tests
├── Mystira.App.Infrastructure.Discord.Tests/ # Discord integration tests
├── DMfinity.Api.Tests/                       # Legacy API tests
├── DMfinity.Domain.Tests/                    # Domain model tests
└── DMfinity.Infrastructure.Azure.Tests/      # Azure services tests
```

### Test Patterns in Use

✅ **Working Well:**
- `CqrsIntegrationTestBase` - Comprehensive base class for CQRS tests
- FluentAssertions for readable assertions
- In-memory database for fast integration tests
- AutoFixture for test data generation

❌ **Needs Improvement:**
- No Blazor component tests (PWA layer untested)
- Inconsistent use of mocking (some tests mock too much, others too little)
- Missing tests for new UX components (LoadingIndicator, ErrorBoundaryWrapper, ToastService)
- No performance/load tests
- No end-to-end tests

---

## Testing Philosophy

### Principles

1. **Test behavior, not implementation**
   - Focus on what the code does, not how it does it
   - Avoid brittle tests that break on refactoring

2. **Arrange-Act-Assert (AAA) pattern**
   - Clear separation of test setup, execution, and verification
   - One logical assertion per test (can have multiple physical assertions for the same concept)

3. **Minimal mocking**
   - Use real implementations where possible (repositories, services)
   - Mock only external dependencies (HTTP, database, file system)
   - Prefer in-memory implementations over mocks

4. **Fast and isolated**
   - Unit tests run in < 5 seconds total
   - Each test is independent (no shared state)
   - Use parallel execution where safe

5. **Readable and maintainable**
   - Descriptive test names (`WhenCondition_ExpectedOutcome`)
   - Clear error messages
   - Avoid complex test logic

---

## Test Pyramid

```
           /\
          /  \         E2E Tests (5%)
         /    \        - Critical user flows
        /------\       - Selenium/Playwright
       /        \
      /  INTE-   \     Integration Tests (25%)
     /  GRATION   \    - API endpoints
    /    TESTS     \   - Database operations
   /--------------  \  - CQRS handlers
  /                  \
 /    UNIT TESTS      \ Unit Tests (70%)
/______________________\ - Domain logic
                         - Business rules
                         - Utilities
```

### Distribution Goals

- **70% Unit Tests**: Fast, focused, testing domain logic and business rules
- **25% Integration Tests**: Testing interactions between components (CQRS, API, database)
- **5% E2E Tests**: Critical user flows (sign up, play scenario, earn badge)

---

## Coverage Goals

### Overall Targets

| Layer | Current | Target Q1 2026 | Target Q2 2026 |
|-------|---------|----------------|----------------|
| **Domain** | ~15% | 90% | 95% |
| **Application (CQRS)** | ~10% | 80% | 90% |
| **API Controllers** | ~5% | 60% | 70% |
| **PWA Components** | 0% | 40% | 60% |
| **Infrastructure** | ~5% | 50% | 60% |
| **Overall** | ~3.7% | 60% | 75% |

### Critical Paths (80%+ Coverage Required)

1. **User Authentication & Authorization**
   - Sign up / sign in
   - Token validation
   - Permission checks

2. **COPPA Compliance**
   - Age verification
   - Parental consent flow
   - Data access controls

3. **Game Session Management**
   - Start session
   - Process choices
   - Calculate compass values
   - Award badges

4. **Content Management**
   - Create/update scenarios
   - Bundle management
   - Media asset handling

5. **User Profile Management**
   - Create/update profiles
   - Character assignments
   - Badge tracking

---

## Testing Patterns

### 1. Domain Model Tests

**What to test:**
- Constructor initialization
- Property setters/getters
- Domain logic methods
- Business rule validation
- Invariants

**Example:**
```csharp
public class GameSessionTests
{
    [Fact]
    public void GetTotalElapsedTime_ReturnsCorrectTime_WhenInProgress()
    {
        // Arrange
        var session = new GameSession
        {
            StartTime = DateTime.UtcNow.AddMinutes(-10),
            Status = SessionStatus.InProgress
        };

        // Act
        var elapsedTime = session.GetTotalElapsedTime();

        // Assert
        elapsedTime.Should().BeCloseTo(TimeSpan.FromMinutes(10), TimeSpan.FromSeconds(1));
    }
}
```

### 2. CQRS Command Tests

**What to test:**
- Command creates/updates entities correctly
- Validation errors throw exceptions
- Data persists to database
- Side effects occur (events, notifications)

**Example:**
```csharp
public class AwardBadgeCommandTests : CqrsIntegrationTestBase
{
    [Fact]
    public async Task AwardBadgeCommand_CreatesNewBadge()
    {
        // Arrange
        await SeedTestDataAsync();
        var command = new AwardBadgeCommand(new AwardBadgeRequest
        {
            UserProfileId = "profile-1",
            BadgeConfigurationId = "badge-config-1",
            Axis = "Courage",
            TriggerValue = 80
        });

        // Act
        var result = await Mediator.Send(command);

        // Assert
        result.Should().NotBeNull();
        result.UserProfileId.Should().Be("profile-1");

        // Verify persistence
        var savedBadge = await DbContext.UserBadges.FindAsync(result.Id);
        savedBadge.Should().NotBeNull();
    }
}
```

### 3. CQRS Query Tests

**What to test:**
- Query returns correct data
- Filtering/sorting works
- Caching behavior
- Cache invalidation

**Example:**
```csharp
[Fact]
public async Task GetUserBadgesQuery_ReturnsCachedResults_OnSecondCall()
{
    // Arrange
    await SeedTestDataAsync();
    var query = new GetUserBadgesQuery("profile-1");

    // Act - First call (should hit database)
    var result1 = await Mediator.Send(query);

    // Act - Second call (should hit cache)
    var result2 = await Mediator.Send(query);

    // Assert
    result1.Should().BeEquivalentTo(result2);
    // Verify cache was used (implementation-specific)
}
```

### 4. API Controller Tests

**What to test:**
- HTTP status codes
- Response shapes
- Authorization attributes
- Request validation
- Error handling

**Example:**
```csharp
[Fact]
public async Task GetMediaById_WhenMediaExists_ReturnsOk()
{
    // Arrange
    var mediator = new Mock<IMediator>();
    mediator.Setup(m => m.Send(It.IsAny<GetMediaAssetQuery>(), default))
        .ReturnsAsync(new MediaAsset { Id = "img-1" });
    var controller = new MediaController(mediator.Object, ...);

    // Act
    var result = await controller.GetMediaById("img-1");

    // Assert
    result.Result.Should().BeOfType<OkObjectResult>();
}
```

### 5. Service Tests

**What to test:**
- Service orchestrates dependencies correctly
- Error handling and retries
- External API calls
- Business logic coordination

**Example:**
```csharp
[Fact]
public async Task ToastService_ShowSuccess_RaisesOnShowEvent()
{
    // Arrange
    var service = new ToastService();
    ToastMessage? capturedMessage = null;
    service.OnShow += (message) => capturedMessage = message;

    // Act
    service.ShowSuccess("Test message");

    // Assert
    capturedMessage.Should().NotBeNull();
    capturedMessage!.Message.Should().Be("Test message");
    capturedMessage.Type.Should().Be(ToastType.Success);
}
```

### 6. Blazor Component Tests (bUnit)

**What to test:**
- Component renders correctly
- User interactions (clicks, input)
- Event callbacks
- Parameter binding
- Lifecycle methods

**Example:**
```csharp
[Fact]
public void LoadingIndicator_WithMessage_RendersMessage()
{
    // Arrange
    using var ctx = new TestContext();

    // Act
    var cut = ctx.RenderComponent<LoadingIndicator>(parameters => parameters
        .Add(p => p.Message, "Loading adventures..."));

    // Assert
    cut.Find(".loading-message").TextContent.Should().Be("Loading adventures...");
}
```

---

## Test Templates

### Template 1: Domain Model Test

**File:** `tests/Mystira.App.Domain.Tests/Models/{EntityName}Tests.cs`

```csharp
using FluentAssertions;
using Mystira.App.Domain.Models;
using Xunit;

namespace Mystira.App.Domain.Tests.Models;

public class {EntityName}Tests
{
    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        // Act
        var entity = new {EntityName}();

        // Assert
        entity.Id.Should().NotBeEmpty();
        // Add more assertions for default values
    }

    [Theory]
    [InlineData(/* test case 1 */)]
    [InlineData(/* test case 2 */)]
    public void Method_WithValidInput_ReturnsExpectedResult(/* parameters */)
    {
        // Arrange
        var entity = new {EntityName} { /* setup */ };

        // Act
        var result = entity.Method(/* args */);

        // Assert
        result.Should().Be(/* expected */);
    }

    [Fact]
    public void Method_WithInvalidInput_ThrowsException()
    {
        // Arrange
        var entity = new {EntityName}();

        // Act & Assert
        var action = () => entity.Method(/* invalid args */);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*expected error message*");
    }
}
```

### Template 2: CQRS Command/Query Test

**File:** `tests/Mystira.App.Application.Tests/CQRS/{Feature}/{CommandName}Tests.cs`

```csharp
using FluentAssertions;
using Mystira.App.Application.CQRS.{Feature}.Commands;
using Mystira.App.Contracts.Requests.{Feature};
using Mystira.App.Domain.Models;
using Xunit;

namespace Mystira.App.Application.Tests.CQRS.{Feature};

public class {CommandName}Tests : CqrsIntegrationTestBase
{
    protected override async Task SeedTestDataAsync()
    {
        // Seed required test data
        var entities = new List<{Entity}>
        {
            new() { Id = "test-1", /* properties */ }
        };
        DbContext.{EntitySet}.AddRange(entities);
        await DbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task Command_WithValidRequest_CreatesEntity()
    {
        // Arrange
        await SeedTestDataAsync();
        var request = new {RequestType} { /* properties */ };
        var command = new {CommandName}(request);

        // Act
        var result = await Mediator.Send(command);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeNullOrEmpty();

        // Verify persistence
        var saved = await DbContext.{EntitySet}.FindAsync(result.Id);
        saved.Should().NotBeNull();
    }

    [Fact]
    public async Task Command_WithInvalidRequest_ThrowsException()
    {
        // Arrange
        await SeedTestDataAsync();
        var request = new {RequestType} { /* invalid properties */ };
        var command = new {CommandName}(request);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await Mediator.Send(command);
        });
    }
}
```

### Template 3: API Controller Test

**File:** `tests/Mystira.App.Api.Tests/Controllers/{ControllerName}Tests.cs`

```csharp
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Mystira.App.Api.Controllers;
using Mystira.App.Contracts.Responses.Common;
using Xunit;

namespace Mystira.App.Api.Tests.Controllers;

public class {ControllerName}Tests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ILogger<{ControllerName}>> _loggerMock;
    private readonly {ControllerName} _controller;

    public {ControllerName}Tests()
    {
        _mediatorMock = new Mock<IMediator>();
        _loggerMock = new Mock<ILogger<{ControllerName}>>();
        _controller = new {ControllerName}(_mediatorMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetById_WhenEntityExists_ReturnsOk()
    {
        // Arrange
        var entityId = "test-id";
        var entity = new {Entity} { Id = entityId };
        _mediatorMock.Setup(m => m.Send(It.IsAny<{QueryType}>(), default))
            .ReturnsAsync(entity);

        // Act
        var result = await _controller.GetById(entityId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var ok = result.Result as OkObjectResult;
        ok!.Value.Should().BeEquivalentTo(entity);
    }

    [Fact]
    public async Task GetById_WhenEntityNotFound_ReturnsNotFound()
    {
        // Arrange
        _mediatorMock.Setup(m => m.Send(It.IsAny<{QueryType}>(), default))
            .ReturnsAsync(({Entity}?)null);

        // Act
        var result = await _controller.GetById("non-existent");

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }
}
```

### Template 4: Service Test

**File:** `tests/Mystira.App.{Layer}.Tests/Services/{ServiceName}Tests.cs`

```csharp
using FluentAssertions;
using Moq;
using Mystira.App.{Layer}.Services;
using Xunit;

namespace Mystira.App.{Layer}.Tests.Services;

public class {ServiceName}Tests
{
    private readonly Mock<{IDependency}> _dependencyMock;
    private readonly {ServiceName} _service;

    public {ServiceName}Tests()
    {
        _dependencyMock = new Mock<{IDependency}>();
        _service = new {ServiceName}(_dependencyMock.Object);
    }

    [Fact]
    public async Task Method_WithValidInput_ReturnsExpectedResult()
    {
        // Arrange
        _dependencyMock.Setup(d => d.Method(It.IsAny<string>()))
            .ReturnsAsync("expected result");

        // Act
        var result = await _service.Method("input");

        // Assert
        result.Should().Be("expected result");
        _dependencyMock.Verify(d => d.Method("input"), Times.Once);
    }

    [Fact]
    public async Task Method_WhenDependencyFails_HandlesError()
    {
        // Arrange
        _dependencyMock.Setup(d => d.Method(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Dependency error"));

        // Act & Assert
        var action = async () => await _service.Method("input");
        await action.Should().ThrowAsync<Exception>()
            .WithMessage("*Dependency error*");
    }
}
```

### Template 5: Blazor Component Test (bUnit)

**File:** `tests/Mystira.App.PWA.Tests/Components/{ComponentName}Tests.cs`

```csharp
using Bunit;
using FluentAssertions;
using Mystira.App.PWA.Components;
using Xunit;

namespace Mystira.App.PWA.Tests.Components;

public class {ComponentName}Tests
{
    [Fact]
    public void Component_WithDefaultParameters_RendersCorrectly()
    {
        // Arrange
        using var ctx = new TestContext();

        // Act
        var cut = ctx.RenderComponent<{ComponentName}>();

        // Assert
        cut.Find(".{expected-class}").Should().NotBeNull();
    }

    [Fact]
    public void Component_WithCustomParameter_RendersParameter()
    {
        // Arrange
        using var ctx = new TestContext();

        // Act
        var cut = ctx.RenderComponent<{ComponentName}>(parameters => parameters
            .Add(p => p.{Parameter}, "Test value"));

        // Assert
        cut.Find(".{selector}").TextContent.Should().Contain("Test value");
    }

    [Fact]
    public void Component_OnButtonClick_InvokesCallback()
    {
        // Arrange
        using var ctx = new TestContext();
        var callbackInvoked = false;
        var cut = ctx.RenderComponent<{ComponentName}>(parameters => parameters
            .Add(p => p.OnClick, () => callbackInvoked = true));

        // Act
        cut.Find("button").Click();

        // Assert
        callbackInvoked.Should().BeTrue();
    }
}
```

---

## Critical Test Paths

### Path 1: User Sign Up & Profile Creation

**Priority:** CRITICAL (COPPA compliance required)

**Test Coverage Required:** 90%+

**Test Cases:**
1. User enters valid email and password → Account created
2. User enters duplicate email → Error displayed
3. User under 13 without parental consent → Blocked
4. User creates first profile → Default avatar assigned
5. Profile name validation → Special characters blocked

**Files to Test:**
- `SignUpCommand.cs`
- `CreateUserProfileCommand.cs`
- `AuthService.cs`
- `ProfileService.cs`

---

### Path 2: Game Session Flow

**Priority:** CRITICAL (core product feature)

**Test Coverage Required:** 85%+

**Test Cases:**
1. User starts new session → Session created, first scene loaded
2. User makes choice → Compass values updated, next scene loaded
3. User earns badge → Badge awarded, notification shown
4. Session reaches end → Status updated to Completed
5. User pauses session → IsPaused = true, elapsed time calculated

**Files to Test:**
- `StartGameSessionCommand.cs`
- `ProcessChoiceCommand.cs`
- `AwardBadgeCommand.cs`
- `GameSessionService.cs`

---

### Path 3: Content Management

**Priority:** HIGH (creator workflows)

**Test Coverage Required:** 75%+

**Test Cases:**
1. Admin creates scenario → Scenario saved with all scenes
2. Admin uploads media → Media stored in Azure Blob, URL returned
3. Admin creates bundle → Bundle contains multiple scenarios
4. Admin updates scenario → Version incremented, changes saved

**Files to Test:**
- `CreateScenarioCommand.cs`
- `UploadMediaCommand.cs`
- `CreateContentBundleCommand.cs`
- `MediaApiService.cs`

---

## Tools & Frameworks

### Testing Frameworks

| Tool | Purpose | Version |
|------|---------|---------|
| **xUnit** | Test runner | 2.6.0+ |
| **FluentAssertions** | Readable assertions | 6.12.0+ |
| **Moq** | Mocking framework | 4.20.0+ |
| **AutoFixture** | Test data generation | 4.18.0+ |
| **bUnit** | Blazor component testing | 1.25.0+ (NEW) |
| **Testcontainers** | Integration testing | 3.6.0+ (FUTURE) |

### Coverage Tools

- **Coverlet**: Code coverage collector
- **ReportGenerator**: Coverage report generation
- **Azure DevOps Coverage**: CI/CD integration

### Command Line Usage

```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

# Run specific test project
dotnet test tests/Mystira.App.Application.Tests/

# Run tests matching filter
dotnet test --filter "FullyQualifiedName~GameSession"

# Generate coverage report
reportgenerator -reports:coverage.opencover.xml -targetdir:coverage-report
```

---

## CI/CD Integration

### Azure DevOps Pipeline

```yaml
# azure-pipelines.yml
trigger:
  - main
  - develop

pool:
  vmImage: 'ubuntu-latest'

steps:
- task: UseDotNet@2
  inputs:
    version: '9.0.x'

- task: DotNetCoreCLI@2
  displayName: 'Restore dependencies'
  inputs:
    command: 'restore'

- task: DotNetCoreCLI@2
  displayName: 'Build solution'
  inputs:
    command: 'build'
    arguments: '--configuration Release --no-restore'

- task: DotNetCoreCLI@2
  displayName: 'Run tests with coverage'
  inputs:
    command: 'test'
    arguments: '--configuration Release --no-build /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura'
    publishTestResults: true

- task: PublishCodeCoverageResults@1
  displayName: 'Publish coverage report'
  inputs:
    codeCoverageTool: 'Cobertura'
    summaryFileLocation: '**/coverage.cobertura.xml'
    reportDirectory: '**/coverage-report'

- task: BuildQualityChecks@8
  displayName: 'Check coverage thresholds'
  inputs:
    checkCoverage: true
    coverageFailOption: 'fixed'
    coverageThreshold: '60'
```

### Quality Gates

- **Minimum coverage:** 60% overall (fails build if below)
- **Critical path coverage:** 80%+ (warning if below)
- **All tests pass:** Required for merge
- **No test warnings:** Flaky tests must be fixed

---

## Best Practices

### DO ✅

1. **Write tests first for new features** (TDD where appropriate)
2. **Test one thing per test** (single responsibility)
3. **Use descriptive test names** (`Method_WhenCondition_ExpectedOutcome`)
4. **Arrange-Act-Assert pattern** (clear structure)
5. **Test edge cases** (null, empty, boundary values)
6. **Clean up resources** (dispose, reset state)
7. **Use FluentAssertions** (readable assertions)
8. **Test public APIs only** (avoid testing private methods)
9. **Keep tests fast** (< 100ms per unit test)
10. **Parallelize where safe** (`[Collection]` for shared resources)

### DON'T ❌

1. **Don't test implementation details** (test behavior)
2. **Don't use Thread.Sleep** (use async/await properly)
3. **Don't share state between tests** (each test is isolated)
4. **Don't mock everything** (use real implementations where fast)
5. **Don't ignore failing tests** (fix or delete)
6. **Don't use magic values** (use constants or test data builders)
7. **Don't test framework code** (trust EF Core, ASP.NET Core)
8. **Don't write tests that can't fail** (verify assertions work)
9. **Don't mix unit and integration tests** (separate test classes)
10. **Don't duplicate test setup** (use base classes, fixtures)

---

## Next Steps

### Phase 1 (Current - Q4 2025)
- ✅ Create testing strategy document
- ⏳ Add bUnit for Blazor component tests
- ⏳ Write tests for new UX components (ToastService, LoadingIndicator, ErrorBoundaryWrapper)
- ⏳ Increase domain layer coverage to 50%

### Phase 2 (Q1 2026)
- Add tests for critical paths (auth, game session, badges)
- Reach 60% overall coverage
- Integrate coverage reports in Azure DevOps
- Create test data builders for common entities

### Phase 3 (Q2 2026)
- Add E2E tests with Playwright
- Reach 75% overall coverage
- Add performance/load tests for API endpoints
- Create mutation testing for critical logic

---

**Questions or Issues?**
See `docs/templates/` for concrete test examples and `tests/` for existing test patterns.
