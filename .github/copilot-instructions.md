# GitHub Copilot Instructions for Mystira.App

## Repository Overview

This is the Mystira Application Suite - a monorepo containing backend APIs, domain libraries, infrastructure components, and a Blazor PWA client. The project follows Hexagonal/Clean Architecture principles with strict layering rules.

## Technology Stack

- **Runtime**: .NET 9 (with .NET SDK 9.0+)
- **Languages**: C# 12, TypeScript
- **Frontend**: Blazor WebAssembly PWA
- **Backend**: ASP.NET Core Web APIs
- **Database**: Azure Cosmos DB (with EF Core provider)
- **Storage**: Azure Blob Storage
- **Build Tools**: dotnet CLI, npm
- **Code Quality**: Husky.Net for pre-commit hooks, dotnet format

## Architecture Principles

### Hexagonal/Clean Architecture Layers

1. **Domain Layer** (`src/Mystira.App.Domain`)
   - Core business models, enumerations, and value objects
   - NO dependencies on other layers
   - Pure business logic only
   - Target: `netstandard2.1`

2. **Application Layer** (`src/Mystira.App.Application`)
   - Use Cases (one class per business action)
   - Application services and orchestration
   - Ports (interfaces for repositories & external systems)
   - Business rules and workflows
   - NO infrastructure concerns

3. **Infrastructure Layer** (`src/Mystira.App.Infrastructure.*`)
   - `Infrastructure.Data`: EF Core DbContext, repositories
   - `Infrastructure.Azure`: Azure-specific implementations (Cosmos DB, Blob Storage)
   - Adapters implementing Application layer ports

4. **API Layer** (`src/Mystira.App.Api` and `src/Mystira.App.Admin.Api`)
   - Controllers ONLY - no business logic
   - DTO binding, routing, auth attributes
   - Maps DTOs to Use Case input models
   - Public API: user-facing endpoints
   - Admin API: system-level operations, moderation, content workflows

5. **PWA Client** (`src/Mystira.App.PWA`)
   - Blazor WebAssembly application
   - Service workers, IndexedDB caching
   - Audio/haptics JS interop, dice utilities

### Critical Dependency Rules

- Domain → NO dependencies
- Application → Domain only
- Infrastructure → Application + Domain
- API/Admin.API → Application + Domain (+ Infrastructure for DI setup)
- PWA → Shared contracts

## Coding Standards

### C# Code Style

- Use `async`/`await` for all I/O operations
- Follow existing naming conventions (PascalCase for public members, _camelCase for private fields)
- Enable nullable reference types (`<Nullable>enable</Nullable>`)
- Use `ImplicitUsings` where enabled
- Target `net9.0` for all new projects (except Domain which uses `netstandard2.1`)

### Security Requirements

- **Never hardcode secrets** - use configuration, environment variables, or Azure Key Vault
- Validate all API inputs at controller level
- Use `[Authorize]` attributes for secured endpoints
- Use `RandomNumberGenerator` (not `Random`) for security-sensitive operations
- Maintain strict CORS whitelist (no wildcards)

### API Controller Pattern

```csharp
[ApiController]
[Route("api/[controller]")]
public class ExampleController : ControllerBase
{
    private readonly IUseCaseInterface _useCase;
    
    public ExampleController(IUseCaseInterface useCase)
    {
        _useCase = useCase;
    }
    
    [HttpPost]
    public async Task<ActionResult<ResponseDto>> Create([FromBody] RequestDto request)
    {
        // 1. Map DTO to use case input
        var input = MapToUseCaseInput(request);
        
        // 2. Call use case (no business logic here)
        var result = await _useCase.ExecuteAsync(input);
        
        // 3. Map result to DTO
        return Ok(MapToResponseDto(result));
    }
}
```

### Use Case Pattern

```csharp
public interface IExampleUseCase
{
    Task<ExampleResult> ExecuteAsync(ExampleInput input);
}

public class ExampleUseCase : IExampleUseCase
{
    private readonly IRepository _repository;
    
    public async Task<ExampleResult> ExecuteAsync(ExampleInput input)
    {
        // Business logic and orchestration here
        // Interact with repositories through interfaces
        var entity = await _repository.GetAsync(input.Id);
        // ... business rules ...
        await _repository.SaveAsync(entity);
        return new ExampleResult { /* ... */ };
    }
}
```

## Project Structure

```
Mystira.App/
├── src/
│   ├── Mystira.App.Domain/           # Core domain models
│   ├── Mystira.App.Application/      # Use cases and business logic
│   ├── Mystira.App.Infrastructure.Data/      # EF Core, repositories
│   ├── Mystira.App.Infrastructure.Azure/    # Azure services
│   ├── Mystira.App.Api/              # Public API
│   ├── Mystira.App.Admin.Api/        # Admin API
│   ├── Mystira.App.PWA/              # Blazor PWA client
│   ├── Mystira.App.CosmosConsole/    # CLI tool for Cosmos operations
│   ├── Mystira.App.Contracts/        # Shared DTOs
│   └── Mystira.App.Shared/           # Shared utilities
├── tests/
│   ├── Mystira.App.Api.Tests/
│   ├── Mystira.App.Admin.Api.Tests/
│   └── *.Tests/
├── docs/                              # Comprehensive documentation
│   ├── architecture/                  # Architecture decisions
│   ├── features/                      # Feature documentation
│   ├── setup/                         # Setup guides
│   └── usecases/                      # Use case documentation
├── .github/
│   └── workflows/                     # CI/CD pipelines
├── .config/
│   └── dotnet-tools.json              # Husky and other tools
└── Mystira.App.sln                    # Solution file
```

## Common Tasks

### Building

```bash
# Build entire solution
dotnet build Mystira.App.sln

# Build specific project
dotnet build src/Mystira.App.Api/Mystira.App.Api.csproj
```

### Testing

```bash
# Run all tests
dotnet test Mystira.App.sln

# Run tests for specific project
dotnet test tests/Mystira.App.Api.Tests/
```

### Code Formatting

```bash
# Format code (also runs automatically via pre-commit hook)
dotnet format Mystira.App.sln
```

### Running Projects

```bash
# Public API
dotnet run --project src/Mystira.App.Api/Mystira.App.Api.csproj

# Admin API
dotnet run --project src/Mystira.App.Admin.Api/Mystira.App.Admin.Api.csproj

# PWA
dotnet run --project src/Mystira.App.PWA/Mystira.App.PWA.csproj

# Cosmos Console
dotnet run --project src/Mystira.App.CosmosConsole/Mystira.App.CosmosConsole.csproj -- export --output data.csv
```

## Documentation

- **Main README**: `/README.md` - Project overview and getting started
- **Contributing**: `/CONTRIBUTING.md` - Contribution guidelines
- **Architecture Rules**: `/docs/architecture/ARCHITECTURAL_RULES.md` - Strict architectural rules
- **Best Practices**: `/docs/best-practices.md` - Development standards
- **API Docs**: `/docs/` - Feature and setup documentation

## Key Conventions

### API Routing

- Public API: `/api/[controller]`
- Admin API: `/api/admin/[controller]` or `/adminapi/[controller]`

### Naming Patterns

- Use Cases: `I[Action][Entity]UseCase` (e.g., `ICreateGameSessionUseCase`)
- Services: `I[Entity]Service` (e.g., `IGameSessionService`)
- Repositories: `I[Entity]Repository` (e.g., `IGameSessionRepository`)
- DTOs: `[Entity][Purpose]` (e.g., `GameSessionResponse`, `CreateGameSessionRequest`)

### Configuration

- Development settings: `appsettings.Development.json`
- User secrets for local development: `dotnet user-secrets`
- Production: Environment variables or Azure Key Vault

## Quality Gates

Before committing:
1. **Build**: `dotnet build Mystira.App.sln` must succeed
2. **Tests**: `dotnet test Mystira.App.sln` must pass
3. **Format**: Automatically enforced via Husky pre-commit hook
4. **PWA Lint** (if JS modified): `cd src/Mystira.App.PWA && npm run lint`

## PII and Security Awareness

- This application handles Personally Identifiable Information (PII) including emails and user aliases
- Follow documented redaction/logging standards
- Ensure PII is not exposed in logs or error messages
- Use secure transfer mechanisms for sensitive data

## When Suggesting Code

1. **Respect the layer boundaries** - don't put business logic in controllers
2. **Use dependency injection** - constructor injection is preferred
3. **Follow async patterns** - use `async`/`await` for I/O
4. **Include proper error handling** - use appropriate exception types
5. **Write tests** - suggest unit tests for use cases, integration tests for APIs
6. **Consider security** - validate inputs, use authorization attributes
7. **Follow existing patterns** - look at similar code in the repository
8. **Update documentation** - suggest doc updates for significant changes

## AI-Specific Guidelines

- When generating controllers, always delegate to use cases - never include business logic
- When generating use cases, include interfaces and follow the existing pattern
- For domain models, ensure they remain infrastructure-agnostic
- Suggest appropriate placement based on architectural rules
- Include necessary using statements and package references
- Follow C# 12 features and .NET 9 best practices
- Consider offline-first patterns for PWA features
