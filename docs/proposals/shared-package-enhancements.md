# Proposal: Mystira.Shared Package Enhancements

**Date:** 2025-12-26
**Status:** Draft
**Related Migration:** MediatR to Wolverine (Wave 4 Complete)

---

## Executive Summary

This proposal outlines enhancements to the `Mystira.Shared` and `Mystira.Contracts` packages to provide standardized patterns for:

1. **Object Mapping** - Mapperly source generator for compile-time mapping
2. **Validation** - FluentValidation abstractions and middleware
3. **CQRS Interfaces** - Optional shared marker interfaces for Wolverine

---

## 1. Mapperly Integration (Medium Priority)

### Problem Statement

Currently, Mystira.App uses manual mapping patterns scattered across handlers:
- 1 dedicated mapper file (`ScenarioMappers.cs`)
- ~20 files with inline `MapTo*` methods
- No compile-time verification of mappings
- Potential for drift between domain entities and DTOs

### Proposed Solution

Add **Mapperly** (a Roslyn source generator) to `Mystira.Shared` for zero-overhead, compile-time object mapping.

### Package Location

```
Mystira.Shared/
├── Mapping/
│   ├── IMapper.cs              # Base marker interface
│   ├── MapperExtensions.cs     # Common extension methods
│   └── README.md               # Usage documentation
```

### Dependencies to Add

```xml
<!-- In Mystira.Shared.csproj -->
<PackageReference Include="Riok.Mapperly" Version="4.1.1" />
```

### Interface Definitions

```csharp
// Mystira.Shared/Mapping/IMapper.cs
namespace Mystira.Shared.Mapping;

/// <summary>
/// Marker interface for Mapperly-generated mappers.
/// Implementations are source-generated at compile time.
/// </summary>
public interface IMapper<TSource, TDestination>
{
    TDestination Map(TSource source);
}

/// <summary>
/// Bidirectional mapper interface for two-way conversions.
/// </summary>
public interface IBidirectionalMapper<T1, T2>
{
    T2 MapForward(T1 source);
    T1 MapReverse(T2 source);
}
```

### Usage Pattern in Mystira.App

```csharp
// In Mystira.App.Application/Mapping/ScenarioMapper.cs
using Riok.Mapperly.Abstractions;
using Mystira.App.Domain.Models;
using Mystira.Contracts.App.Responses.Scenarios;

namespace Mystira.App.Application.Mapping;

[Mapper]
public static partial class ScenarioMapper
{
    // Auto-mapped by convention (matching property names)
    public static partial ScenarioResponse ToResponse(this Scenario entity);

    // Custom mapping for nested objects
    [MapProperty(nameof(Scenario.Scenes), nameof(ScenarioResponse.SceneCount))]
    public static partial ScenarioSummaryResponse ToSummary(this Scenario entity);

    // Request to domain entity
    public static partial Scenario ToDomain(this CreateScenarioRequest request);
}

// Usage in handlers:
public static class GetScenarioQueryHandler
{
    public static async Task<ScenarioResponse?> Handle(
        GetScenarioQuery query,
        IScenarioRepository repository,
        CancellationToken ct)
    {
        var scenario = await repository.GetByIdAsync(query.ScenarioId);
        return scenario?.ToResponse();  // Extension method from mapper
    }
}
```

### Benefits

| Feature | AutoMapper | Mapperly |
|---------|------------|----------|
| Runtime Overhead | Reflection-based | Zero (compile-time) |
| AOT Compatible | Limited | Full support |
| Debugging | Opaque | View generated code |
| IDE Support | Runtime errors | Compile-time errors |
| Configuration | Runtime profiles | Attributes |
| .NET 9 Native AOT | Requires config | Works out of box |

### Migration Path

1. Add Mapperly to `Mystira.Shared`
2. Create mapper classes in `Mystira.App.Application/Mapping/`
3. Gradually replace inline `MapTo*` methods with generated mappers
4. Remove manual mapping code as mappers are verified

---

## 2. FluentValidation Middleware (Low Priority)

### Problem Statement

Current validation in Mystira.App:
- Imperative `if/throw` statements in handlers
- JSON Schema validation for complex scenarios
- No centralized validation pipeline
- Inconsistent error response format

### Proposed Solution

Add FluentValidation abstractions to `Mystira.Shared` with Wolverine middleware integration.

### Package Location

```
Mystira.Shared/
├── Validation/
│   ├── IValidatable.cs                # Marker for auto-validated messages
│   ├── ValidationResult.cs            # Standardized result type
│   ├── ValidationMiddleware.cs        # Wolverine middleware
│   └── ValidationExtensions.cs        # DI registration helpers
```

### Dependencies

```xml
<!-- In Mystira.Shared.csproj -->
<PackageReference Include="FluentValidation" Version="11.11.0" />
<PackageReference Include="FluentValidation.DependencyInjectionExtensions" Version="11.11.0" />
```

### Core Abstractions

```csharp
// Mystira.Shared/Validation/IValidatable.cs
namespace Mystira.Shared.Validation;

/// <summary>
/// Marker interface for messages that should be automatically validated
/// by the validation middleware before handler execution.
/// </summary>
public interface IValidatable { }

// Mystira.Shared/Validation/ValidationResult.cs
namespace Mystira.Shared.Validation;

/// <summary>
/// Standardized validation result that maps to RFC 7807 Problem Details.
/// </summary>
public sealed record ValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public IReadOnlyList<ValidationError> Errors { get; init; } = [];

    public static ValidationResult Success() => new();
    public static ValidationResult Failure(IEnumerable<ValidationError> errors)
        => new() { Errors = errors.ToList() };
}

public sealed record ValidationError(
    string PropertyName,
    string ErrorMessage,
    string? ErrorCode = null,
    object? AttemptedValue = null);
```

### Wolverine Middleware

```csharp
// Mystira.Shared/Validation/ValidationMiddleware.cs
using FluentValidation;
using Wolverine;

namespace Mystira.Shared.Validation;

/// <summary>
/// Wolverine middleware that validates IValidatable messages before handler execution.
/// Throws ValidationException if validation fails.
/// </summary>
public class ValidationMiddleware<TMessage> where TMessage : IValidatable
{
    public static async Task<HandlerContinuation> BeforeAsync(
        TMessage message,
        IEnumerable<IValidator<TMessage>> validators,
        CancellationToken ct)
    {
        if (!validators.Any())
            return HandlerContinuation.Continue;

        var context = new ValidationContext<TMessage>(message);
        var results = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, ct)));

        var failures = results
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .Select(f => new ValidationError(
                f.PropertyName,
                f.ErrorMessage,
                f.ErrorCode,
                f.AttemptedValue))
            .ToList();

        if (failures.Count > 0)
        {
            throw new Mystira.App.Domain.Exceptions.ValidationException(
                failures.Select(f => new Mystira.App.Domain.Exceptions.ValidationError(
                    f.PropertyName, f.ErrorMessage)));
        }

        return HandlerContinuation.Continue;
    }
}
```

### DI Registration

```csharp
// Mystira.Shared/Validation/ValidationExtensions.cs
namespace Mystira.Shared.Validation;

public static class ValidationExtensions
{
    /// <summary>
    /// Registers FluentValidation validators from the specified assembly
    /// and configures Wolverine validation middleware.
    /// </summary>
    public static IServiceCollection AddValidation<TAssemblyMarker>(
        this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<TAssemblyMarker>();
        return services;
    }
}
```

### Usage in Mystira.App

```csharp
// Command with validation marker
public record CreateScenarioCommand(CreateScenarioRequest Request)
    : ICommand<Scenario>, IValidatable;

// Validator class (auto-discovered)
public class CreateScenarioCommandValidator : AbstractValidator<CreateScenarioCommand>
{
    public CreateScenarioCommandValidator()
    {
        RuleFor(x => x.Request.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters");

        RuleFor(x => x.Request.AgeGroup)
            .NotEmpty().WithMessage("Age group is required")
            .Must(BeValidAgeGroup).WithMessage("Invalid age group");
    }

    private bool BeValidAgeGroup(string? ageGroup)
        => Enum.TryParse<AgeGroup>(ageGroup, out _);
}

// Program.cs registration
builder.Services.AddValidation<CreateScenarioCommandValidator>();

// Wolverine configuration
builder.Host.UseWolverine(opts =>
{
    opts.Policies.ForMessagesOfType<IValidatable>()
        .AddMiddleware(typeof(ValidationMiddleware<>));
});
```

### Benefits

- Declarative validation rules
- Automatic validation before handler execution
- Consistent error responses via GlobalExceptionHandler
- Testable validators in isolation
- Reusable validation rules

---

## 3. CQRS Marker Interfaces (Optional)

### Problem Statement

Current ICommand/IQuery interfaces in `Mystira.App.Application/CQRS/`:
- 143 files reference these interfaces
- Purely semantic markers (Wolverine doesn't require them)
- Cannot be reused in other Mystira.Workspace apps

### Proposed Solution

Optionally move CQRS marker interfaces to `Mystira.Shared` for workspace-wide consistency.

### Package Location

```
Mystira.Shared/
├── CQRS/
│   ├── ICommand.cs     # Command marker interfaces
│   ├── IQuery.cs       # Query marker interface
│   └── README.md       # CQRS pattern documentation
```

### Interface Definitions

```csharp
// Mystira.Shared/CQRS/ICommand.cs
namespace Mystira.Shared.CQRS;

/// <summary>
/// Marker interface for commands (write operations) that return a result.
/// Commands modify state and should be idempotent when possible.
/// Used with Wolverine for message-based CQRS pattern.
/// </summary>
/// <typeparam name="TResponse">The type of response returned by the command</typeparam>
public interface ICommand<out TResponse>
{
}

/// <summary>
/// Marker interface for commands (write operations) that don't return a result.
/// Commands modify state and should be idempotent when possible.
/// </summary>
public interface ICommand
{
}

// Mystira.Shared/CQRS/IQuery.cs
namespace Mystira.Shared.CQRS;

/// <summary>
/// Marker interface for queries (read operations).
/// Queries should NOT modify state.
/// Used with Wolverine for message-based CQRS pattern.
/// </summary>
/// <typeparam name="TResponse">The type of response returned by the query</typeparam>
public interface IQuery<out TResponse>
{
}
```

### Migration Path (If Adopted)

1. Add interfaces to `Mystira.Shared/CQRS/`
2. Update `Mystira.App.Application` to reference `Mystira.Shared.CQRS`
3. Change `using Mystira.App.Application.CQRS;` to `using Mystira.Shared.CQRS;`
4. Delete local interface files
5. ~143 files need namespace update (can be done with find/replace)

### Recommendation

**Keep local unless:**
- Other apps in Mystira.Workspace will adopt Wolverine + CQRS
- You want enforced consistency across the workspace
- You plan to share command/query types between apps

Since Wolverine discovers handlers by convention (not interface), these interfaces are purely organizational. The cost of moving them is low, but the benefit is also low unless you have multi-app CQRS scenarios.

---

## Implementation Priority

| Enhancement | Priority | Effort | Dependencies | Recommendation |
|-------------|----------|--------|--------------|----------------|
| Mapperly | Medium | 2-3 hours | None | **Implement** - Clear benefits |
| FluentValidation | Low | 4-6 hours | Domain exceptions | **Consider** - Nice to have |
| CQRS Interfaces | Optional | 1 hour | None | **Defer** - Low value |

---

## Package Version Updates Required

If implementing all enhancements, update `Mystira.Shared.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <PackageId>Mystira.Shared</PackageId>
    <Version>0.3.0-alpha</Version>  <!-- Bump for new features -->
  </PropertyGroup>

  <ItemGroup>
    <!-- Existing dependencies -->
    <PackageReference Include="Polly" Version="8.5.2" />
    <PackageReference Include="Microsoft.Extensions.Caching.Memory" Version="9.0.0" />

    <!-- New: Mapping -->
    <PackageReference Include="Riok.Mapperly" Version="4.1.1" />

    <!-- New: Validation (optional) -->
    <PackageReference Include="FluentValidation" Version="11.11.0" />
    <PackageReference Include="FluentValidation.DependencyInjectionExtensions" Version="11.11.0" />
  </ItemGroup>
</Project>
```

---

## Next Steps

1. **Review** this proposal and provide feedback
2. **Decide** which enhancements to implement
3. **Update** `Mystira.Shared` package in Mystira.Workspace repo
4. **Publish** new version (e.g., `0.3.0-alpha-dev.1`)
5. **Update** `Mystira.App` to use new package version
6. **Migrate** existing code to use shared patterns

---

## Appendix: File Changes Summary

### If Implementing Mapperly Only

**Mystira.Shared:**
```
+ Mapping/IMapper.cs
+ Mapping/README.md
~ Mystira.Shared.csproj (add Mapperly)
```

**Mystira.App:**
```
+ Application/Mapping/ScenarioMapper.cs
+ Application/Mapping/AccountMapper.cs
+ Application/Mapping/GameSessionMapper.cs
~ 20+ handlers (use generated mappers)
- Application/Mappers/ScenarioMappers.cs (replace with generated)
```

### If Implementing All

**Mystira.Shared:**
```
+ Mapping/IMapper.cs
+ Mapping/README.md
+ Validation/IValidatable.cs
+ Validation/ValidationResult.cs
+ Validation/ValidationMiddleware.cs
+ Validation/ValidationExtensions.cs
+ Validation/README.md
+ CQRS/ICommand.cs
+ CQRS/IQuery.cs
+ CQRS/README.md
~ Mystira.Shared.csproj
```

**Mystira.App:**
```
+ Application/Mapping/*.cs
+ Application/Validators/*.cs
~ ~143 files (CQRS namespace change)
~ Program.cs (validation registration)
- Application/CQRS/ICommand.cs
- Application/CQRS/IQuery.cs
```
