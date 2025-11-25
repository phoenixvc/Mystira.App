---
applyTo: "**/tests/**/*.cs"
---

# Test Guidelines

## Framework

- Use **xUnit** for all tests
- Use **Moq** for mocking dependencies
- Use **FluentAssertions** where available

## Test Structure

```csharp
public class MyUseCaseTests
{
    [Fact]
    public async Task MethodName_Scenario_ExpectedResult()
    {
        // Arrange
        var mockRepo = new Mock<IRepository>();
        var sut = new MyUseCase(mockRepo.Object);

        // Act
        var result = await sut.ExecuteAsync(input);

        // Assert
        Assert.NotNull(result);
        mockRepo.Verify(r => r.Method(), Times.Once);
    }
}
```

## Naming Convention

Use pattern: `MethodName_Scenario_ExpectedResult`

## Coverage Targets

- **Overall**: 60%+
- **Critical paths**: 80%+
- **Domain layer**: 90%+

## Priority Areas

1. Authentication flows
2. Game session management
3. COPPA/parental consent
4. Repository implementations
5. Use cases
